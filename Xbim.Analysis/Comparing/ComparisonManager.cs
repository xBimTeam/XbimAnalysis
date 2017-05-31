using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.HSSF.Util;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Analysis.Comparing
{
    public class ComparisonManager
    {
        private List<IModelComparerII> _comparers = new List<IModelComparerII>();
        private ComparisonResultsCollection _results = new ComparisonResultsCollection();
        private IModel _baseModel;
        private IModel _revisedModel;

        public ComparisonResultsCollection Results { get { return _results; } }

        public ComparisonManager(IModel baseModel, IModel revisedModel)
        {
            _baseModel = baseModel;
            _revisedModel = revisedModel;
        }

        public void AddComparer(IModelComparerII comparer)
        {
            if (comparer == null) throw new ArgumentNullException();
            if (!_comparers.Contains(comparer))
                _comparers.Add(comparer);

        }

        public void Compare<T>() where T : IIfcRoot
        {
            _results = new ComparisonResultsCollection();
            var baselineRoots = _baseModel.Instances.OfType<T>().ToList();

            //perform all the comparisons in parallel
            //foreach (var comparer in _comparers)
            var opts = new ParallelOptions() {MaxDegreeOfParallelism = 8 };
            Parallel.ForEach<IModelComparerII>(_comparers, opts, comparer =>
            {
                foreach (var root in baselineRoots)
                {
                    var result = comparer.Compare<T>(root, _revisedModel);
                    //result can be null if there is no sense in comparison
                    //like comparison of geometry when there is no geometry at all
                    if (result != null)
                        lock (_results)
                        {
                            _results.Add(result);   
                        }
                }

                //get objects which are supposed to be new
                var residuum = comparer.GetResidualsFromRevision<T>(_revisedModel);
                lock (_results)
                {
                    _results.Add(residuum);
                }
            }
            );

            //clean up candidaes which are supposed to be new but they are already somewhere else
            var added = _results.Added.ToList();
            foreach (var candidate in added)
            {
                foreach (var item in _results.MatchOneToOne)
                {
                    //object is already assigned somewhere else as a match one to one which takes precedence
                    //so it should be removed from here
                    if (item.Value == candidate)
                    {
                        //remove from results
                        foreach (var result in _results.Results.Where(r => r.Baseline == null))
                            foreach (var res in result.Results)
                                if (res.Candidates.Contains(candidate))
                                    res.Candidates.Remove(candidate);
                    }
                }
            }

            //clean up candidaes which are supposed to be new but they are already somewhere else
            var ambiquities = _results.Ambiquity.ToList();
            foreach (var candidate in ambiquities)
            {
                foreach (var match in _results.MatchOneToOne)
                {
                    //ambiquity should be removed as it is already good match for some other element
                    var toRemove = candidate.Value.Where(c => c.Candidate == match.Value);
                    if (toRemove.Any()) 
                    {
                        var rootToRemove = toRemove.FirstOrDefault().Candidate;
                        //remove from results
                        foreach (var result in _results.Results.Where(r => r.Baseline == candidate.Key))
                            foreach (var res in result.Results)
                                if (res.Candidates.Contains(rootToRemove))
                                    res.Candidates.Remove(rootToRemove);
                    }
                }
            }
        }

        public void SaveResultToCSV(string path)
        {
            if (path == null)
                throw new ArgumentNullException();

            var ext = Path.GetExtension(path).ToLower();
            if (ext != ".csv")
                path = path + ".csv";

            var file = File.CreateText(path);

            //create header
            file.Write("{0},{1}","Baseline", "Revision");
            foreach (var cmp in _comparers)
            {
                file.Write(",{0} (Weight: {1})", cmp.ComparisonType, cmp.Weight);
            }
            file.Write(",Match\n");

            //write content
            foreach (var item in _results.Results)
            {
                var baseLabel = item.Baseline != null ? "#" + item.Baseline.EntityLabel.ToString() : "-";
                var bestMatch = item.BestMatch;

                if (bestMatch.Count == 0)
                {
                    file.Write("{0},{1}", baseLabel, "-");
                    foreach (var cmp in _comparers)
                        file.Write("," + ResultType.ONLY_BASELINE);
                    file.Write(",100%\n");
                    continue;
                }

                foreach (var match in item.BestMatch)
                {
                    var matchLabel = "#" + match.Candidate.EntityLabel.ToString();
                    file.Write("{0},{1}", baseLabel, matchLabel);
                    foreach (var cmp in _comparers)
                    {
                        var results = item.Results.Where(r => r.Comparer == cmp);
                        if (results.Count() == 0)
                            file.Write(",-");
                        else
                        {
                            var result = results.Where(r => r.Candidates.Contains(match.Candidate)).FirstOrDefault();
                            file.Write("," + (result != null ? "true" : "false"));
                        }
                    }
                    var relWeight = (float)(match.Weight) / (float)(item.MaximalWeight) * 100f;
                    file.WriteLine(",{0,-1:f2}%", relWeight);
                }
            }
            file.Close();
        }

        public void SaveResultToXLS(string outputPath)
        {
            if (String.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException();

            var ext = Path.GetExtension(outputPath).ToLower();
            if (ext != ".xls")
                outputPath = outputPath + ".xls";

            HSSFWorkbook workbook = null;
            try
            {
                workbook = new HSSFWorkbook();
                var index = 0;
                var rowNum = 0;

                // Getting the worksheet by its name... 
                ISheet sheet = workbook.CreateSheet("Comparison_results");

                //create header
                var headerStyle = GetCellStyle(workbook, "", GetColor(workbook, 192, 192, 192));
                IRow dataRow = sheet.CreateRow(rowNum);
                var cellBase = dataRow.CreateCell(index, CellType.String);
                cellBase.SetCellValue("Baseline label");
                cellBase.CellStyle = headerStyle;

                var cellType = dataRow.CreateCell(++index, CellType.String);
                cellType.SetCellValue("Baseline type");
                cellType.CellStyle = headerStyle;

                var cellLabel = dataRow.CreateCell(++index, CellType.String);
                cellLabel.SetCellValue("Revision label");
                cellLabel.CellStyle = headerStyle;

                cellType = dataRow.CreateCell(++index, CellType.String);
                cellType.SetCellValue("Revision type");
                cellType.CellStyle = headerStyle;
                

                foreach (var cmp in _comparers)
                {
                    var cell = dataRow.CreateCell(++index, CellType.String);
                    cell.SetCellValue(String.Format("{0} (Weight: {1})", cmp.ComparisonType, cmp.Weight));
                    cell.CellStyle = headerStyle;
                }
                var cellMatch = dataRow.CreateCell(++index, CellType.String);
                cellMatch.SetCellValue("Match rate");
                cellMatch.CellStyle = headerStyle;

                //write content
                foreach (var item in _results.Results)
                {

                    var baseLabel = item.Baseline != null ? "#" +item.Baseline.EntityLabel.ToString() : "";
                    var baseType = item.Baseline != null ? item.Baseline.GetType().Name : "";
                    var bestMatch = item.BestMatch;

                    HSSFColor color = GetColor(workbook, 255, 255, 255);
                    if (bestMatch.Count == 0 && item.Baseline != null) //deleted
                        color = GetColor(workbook, 255, 0, 0);
                    if (item.Baseline == null)                          //added
                        color = GetColor(workbook, 0, 255, 0);
                    if (item.Baseline != null && bestMatch.Count > 1) //ambiquity
                        color = GetColor(workbook, 0, 0, 255);
                    var weightRate = (float)item.BestMatchWeight / (float)item.MaximalWeight;
                    if (item.Baseline != null && bestMatch.Count == 1 && 
                        (weightRate < 0.99999)) //changed
                        color = GetColor(workbook, 255, 255, 0);

                    var style = GetCellStyle(workbook, "", color);
                    var redStyle = GetCellStyle(workbook, "", GetColor(workbook, 255, 0, 0));
                    var percentStyle = GetCellStyle(workbook, "0.00%", color);

                    if (bestMatch.Count == 0 && item.Baseline != null)
                    {
                        dataRow = sheet.CreateRow(++rowNum);
                        index = 0;

                        var c1 = dataRow.CreateCell(index, CellType.String);
                        c1.SetCellValue(baseLabel);
                        c1.CellStyle = style;

                        var c2 = dataRow.CreateCell(++index, CellType.String);
                        c2.SetCellValue(baseType);
                        c2.CellStyle = style;

                        var ct = dataRow.CreateCell(++index, CellType.String);
                        ct.SetCellValue("");
                        ct.CellStyle = style;

                        ct = dataRow.CreateCell(++index, CellType.String);
                        ct.SetCellValue("");
                        ct.CellStyle = style;

                        foreach (var cmp in _comparers)
                        {
                            var c = dataRow.CreateCell(++index, CellType.String);
                            c.SetCellValue("");
                            c.CellStyle = style;
                            var comparers = item.Results.Where(r => r.Comparer == cmp); //check is this comparer did return any result for this
                            if (comparers.Any())
                                c.SetCellValue("false");
                        }

                        var c3 = dataRow.CreateCell(++index, CellType.Numeric);
                        c3.SetCellValue(0);
                        c3.CellStyle = percentStyle;
                    }
                    else
                    {
                        var collection = item.Baseline == null ? item.WeightedResults : bestMatch;
                        foreach (var match in collection)
                        {
                            dataRow = sheet.CreateRow(++rowNum);
                            index = 0;

                            var c1 = dataRow.CreateCell(index, CellType.String);
                            c1.SetCellValue(baseLabel);
                            c1.CellStyle = style;

                            c1 = dataRow.CreateCell(++index, CellType.String);
                            c1.SetCellValue(baseType);
                            c1.CellStyle = style;

                            var matchLabel = match.Candidate != null ? "#" +match.Candidate.EntityLabel.ToString() : "";
                            var matchType = match.Candidate != null ? match.Candidate.GetType().Name : "";
                            var c2 = dataRow.CreateCell(++index, CellType.String);
                            c2.SetCellValue(matchLabel);
                            c2.CellStyle = style;

                            var ct = dataRow.CreateCell(++index, CellType.String);
                            ct.SetCellValue(matchType);
                            ct.CellStyle = style;

                            foreach (var cmp in _comparers)
                            {
                                var c = dataRow.CreateCell(++index, CellType.String);
                                c.SetCellValue("");
                                c.CellStyle = style;

                                var comparers = item.Results.Where(r => r.Comparer == cmp); //check is this comparer did return any result for this
                                if (comparers.Any() && item.Baseline != null)
                                {
                                    var result = comparers.Where(r => r.Candidates.Contains(match.Candidate));
                                    c.SetCellValue(result.Any() ? "true" : "false");
                                    if (!result.Any())
                                        c.CellStyle = redStyle;
                                }
                            }

                            var relWeight = (float)(match.Weight) / (float)(item.MaximalWeight);
                            var c3 = dataRow.CreateCell(++index, CellType.Numeric);
                            c3.SetCellValue(relWeight);
                            c3.CellStyle = percentStyle;
                            if (item.Baseline == null)
                                c3.SetCellValue(0);
                        }
                    }
  
                }

            }
            catch (Exception e)
            {
                throw new Exception("It wasn't possible to save comparers to spreadsheet.", e);
            }
            finally
            {
                var file = File.Create(outputPath);
                if (workbook != null)
                    workbook.Write(file);
                file.Close();
            }
        }

        private HSSFColor GetColor(HSSFWorkbook workbook, byte red, byte green, byte blue)
        {
            HSSFPalette palette = workbook.GetCustomPalette();
            HSSFColor colour = palette.FindSimilarColor(red, green, blue);
            if (colour == null)
            {
                // NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE is now const...
                //// First 64 are system colours
                //if (NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE < 64)
                //{
                //    NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE = 64;
                //}
                //NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE++;
                colour = palette.AddColor(red, green, blue);
            }
            return colour;
        }

        private HSSFCellStyle GetCellStyle(HSSFWorkbook workbook, string formatString, HSSFColor colour)
        {
            HSSFCellStyle cellStyle;
            cellStyle = workbook.CreateCellStyle() as HSSFCellStyle;

            HSSFDataFormat dataFormat = workbook.CreateDataFormat() as HSSFDataFormat;
            cellStyle.DataFormat = dataFormat.GetFormat(formatString);

            cellStyle.FillForegroundColor = colour.Indexed;
            cellStyle.FillPattern = FillPattern.SolidForeground;

            //cellStyle.BorderBottom = BorderStyle.THIN;
            //cellStyle.BorderLeft = BorderStyle.THIN;
            //cellStyle.BorderRight = BorderStyle.THIN;
            //cellStyle.BorderTop = BorderStyle.THIN;

            return cellStyle;
        }

    }

    /// <summary>
    /// Collection of comparison results. This class keeps detail results where
    /// there are baseline objects and lists of candidates for every comparison 
    /// criterium. You can either perform your own interpretation of the results
    /// or use predefined properties to get new, deleted, matched and ambiquous 
    /// objects from the baseline and revised models.
    /// </summary>
    public class ComparisonResultsCollection
    {
        private List<ComparisonResults> _results = new List<ComparisonResults>();
        public IEnumerable<ComparisonResults> Results { get { return _results; } }

        /// <summary>
        /// Add new comparison result to the collection
        /// </summary>
        /// <param name="result"></param>
        public void Add(ComparisonResult result)
        {
            var baseline = result.Baseline;
            var item = _results.Where(i => i.Baseline == baseline).FirstOrDefault();
            if (item != null)
                item.Add(result);
            else
            {
                var cr = new ComparisonResults();
                cr.Add(result);
                _results.Add(cr);
            }
        }

        /// <summary>
        /// Objects from baseline model which are not present in revision
        /// </summary>
        public IEnumerable<IIfcRoot> Deleted
        {
            get 
            {
                foreach (var result in this._results)
                {
                    if (result.BestMatch.Count == 0 && result.Baseline != null)
                        yield return result.Baseline;
                }
            }
        }

        /// <summary>
        /// Objects from revision which are not present in baseline model
        /// </summary>
        public IEnumerable<IIfcRoot> Added
        {
            get
            {
                foreach (var result in this._results.Where(r => r.Baseline == null))
                {
                    foreach (var item in result.WeightedResults)
                    {
                        yield return item.Candidate;
                    }
                }
            }
        }

        /// <summary>
        /// Dictionary of matching objects where key is 
        /// from baseline model and value is from revision
        /// </summary>
        public IDictionary<IIfcRoot, IIfcRoot> MatchOneToOne
        {
            get 
            {
                var res = new Dictionary<IIfcRoot, IIfcRoot>();
                foreach (var result in this._results)
                {
                    var bestMatch = result.BestMatch;
                    if (result.Baseline != null && bestMatch.Count == 1)
                        res.Add(result.Baseline, bestMatch[0].Candidate);
                }
                return res;
            }
        }

        /// <summary>
        /// Dictionary of baseline objects and weighted objects 
        /// from revision where there are more than one best match
        /// in the revision model. Weights are based on the weights
        /// of comparers.
        /// </summary>
        public IDictionary<IIfcRoot, List<WeightedRoot>> Ambiquity
        {
            get
            {
                var res = new Dictionary<IIfcRoot, List<WeightedRoot>>();
                foreach (var result in this._results)
                {
                    var bestMatch = result.BestMatch;
                    if (result.Baseline != null && bestMatch.Count > 1)
                        res.Add(result.Baseline, bestMatch);
                }
                return res;
            }
        }

    }

    public class ComparisonResults
    {
        private List<ComparisonResult> _results = new List<ComparisonResult>();
        public IEnumerable<ComparisonResult> Results
        {
            get
            {
                foreach (var item in _results)
                {
                    yield return item;
                }
            }
        }

        public IIfcRoot Baseline
        {
            get
            {
                var first = _results.FirstOrDefault();
                return first != null ? _results.FirstOrDefault().Baseline : null;
            }
        }

        public void Add(ComparisonResult result)
        {
            if (Baseline == null)
            {
                _results.Add(result);
                return;
            }
            if (Baseline == result.Baseline)
                _results.Add(result);
            else
                throw new ArgumentException("Result must have the same baseline object");
        }

        public ResultType ResultType
        {
            get
            {
                var best = BestMatch;
                var weightedResults = WeightedResults;
                if (best.Count == 1 && Baseline != null) return ResultType.MATCH;
                if (Baseline != null && best.Count == 0) return ResultType.ONLY_BASELINE;
                if (Baseline == null && weightedResults.Count > 0) return ResultType.ONLY_REVISION;
                if (Baseline != null && best.Count > 1) return ResultType.AMBIGUOUS;
                return ResultType.AMBIGUOUS;
            }
        }

        public List<WeightedRoot> WeightedResults 
        {
            get 
            {
                var weightedResults = new List<WeightedRoot>();
                foreach (var result in _results)
                {
                    foreach (var item in result.Candidates)
                    {
                        var existing = weightedResults.Where(wr => wr.Candidate == item).FirstOrDefault();
                        if (existing != null)
                            existing.Weight += result.Comparer.Weight;
                        else
                            weightedResults.Add(new WeightedRoot() { Candidate = item, Weight = result.Comparer.Weight });
                    }
                }
                //sort result
                weightedResults.Sort();

                return weightedResults;
            }
        }

        public List<WeightedRoot> BestMatch
        {
            get 
            {
                var weightedResults = WeightedResults;
                if (weightedResults.Count == 0)
                    return new List<WeightedRoot>();
                var weight = weightedResults.Last().Weight;
                return weightedResults.Where(wr => wr.Weight == weight).ToList();
            }
        }

        public int MaximalWeight
        {
            get 
            {
                var w = 0;
                foreach (var result in _results)
                    w += result.Comparer.Weight;
                return w;
            }
        }

        public int BestMatchWeight
        {
            get
            {
                var wr = WeightedResults;
                if (wr.Count == 0)
                    return 0;
                return wr.Last().Weight;
            }
        }

        
    }

    public class WeightedRoot : IComparable
    {
        public int Weight;
        public IIfcRoot Candidate;

        public int CompareTo(object obj)
        {
            var second = obj as WeightedRoot;
            if (second == null)
                throw new NotImplementedException();
            return Weight.CompareTo(second.Weight);
        }
    }
}
