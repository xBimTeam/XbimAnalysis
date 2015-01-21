using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;

namespace Xbim.Analysis.Comparing
{
    public interface IModelComparerII
    {
        /// <summary>
        /// Descriptive name of the comparer. This can be used for identification of the comparer
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of the comparer. This can be used when results are to be interpreted.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Type of the comparison
        /// </summary>
        ComparisonType ComparisonType { get; }

        /// <summary>
        /// Weight of the criterium of this comparer. This will be used for weighted evaluation of the results.
        /// Comparer should have its default weight different from 0.
        /// </summary>
        int Weight { get; set; }

        /// <summary>
        /// Gets candidates for the match for the baseline from the revised model 
        /// </summary>
        /// <param name="baseline">object from the baseline model</param>
        /// <param name="revisedModel">revised model</param>
        /// <returns></returns>
        ComparisonResult Compare<T>(T baseline, XbimModel revisedModel) where T : IfcRoot;

        /// <summary>
        /// Gets all objects of the specified type which were not returned in any comparison result before
        /// </summary>
        /// <typeparam name="T">Type to search for. It should be the same type used before in Compare() function.</typeparam>
        /// <param name="revisedModel">Revised model</param>
        /// <returns>Comparison result with null baseline</returns>
        ComparisonResult GetResidualsFromRevision<T>(XbimModel revisedModel) where T : IfcRoot;

        /// <summary>
        /// Gets overall comparison of the base model and its revised version
        /// </summary>
        /// <typeparam name="T">Type of the objects to be compared</typeparam>
        /// <param name="baseline">Baseline model</param>
        /// <param name="revisedModel">Revised model</param>
        /// <returns></returns>
        IEnumerable<ComparisonResult> Compare<T>(XbimModel baseline, XbimModel revised) where T : IfcRoot;

        /// <summary>
        /// Gets difference between two objects. This is to be used to explore the difference in detail. This 
        /// function is not to be used for comparison of two models item by item as it will be more resource
        /// consuming than Compare() method which can take advantege from caching and other techniques.
        /// </summary>
        /// <param name="baseline">Baseline object</param>
        /// <param name="revision">Revised object which is supposed to be the match of the baseline</param>
        /// <returns>Enumeration of the differences like list of changed attributes or properties.</returns>
        IEnumerable<Difference> GetDifferences(IfcRoot baseline, IfcRoot revision);
    }

    /// <summary>
    /// This object holds two version of the object like attribute, property or similar. 
    /// It is to be used for detailed comparison of two objects.
    /// </summary>
    public class Difference
    {
        //original object
        public object OriginalValue;
        //changed object
        public object RevisedValue;

        //field of the object might be specified. It can be used in reflection to show differences
        public string FieldName;

        //General message about the difference
        public string Message;
    }

    /// <summary>
    /// Result of the comparison. It contains Baseline which represents original value, candidates for the match based on the
    /// criterum applied by the comparer and the comparer itself which can tell what kind of comparison was done to get 
    /// this result.
    /// </summary>
    public class ComparisonResult
    {
        private IfcRoot _baseline;
        /// <summary>
        /// Baseline of the comparison
        /// </summary>
        public IfcRoot Baseline { get { return _baseline; } }

        private ComparisonCandidates _candidates = new ComparisonCandidates();
        /// <summary>
        /// Candodates for the match
        /// </summary>
        public ComparisonCandidates Candidates { get { return _candidates; } }

        //this is to identify origin of the comparison
        private IModelComparerII _comparer;
        public IModelComparerII Comparer { get { return _comparer; } }

        public ComparisonResult(IfcRoot baseline, IModelComparerII comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            _baseline = baseline;
            _comparer = comparer;
        }

        public ResultType ResultType
        {
            get
            {
                if (_baseline != null && _candidates.Count == 1) return Comparing.ResultType.MATCH;
                if (_baseline != null && _candidates.Count == 0) return Comparing.ResultType.ONLY_BASELINE;
                if (_baseline == null && _candidates.Count > 0) return Comparing.ResultType.ONLY_REVISION;
                if (_baseline != null && _candidates.Count > 1) return Comparing.ResultType.AMBIGUOUS;
                return Comparing.ResultType.AMBIGUOUS;
            }
        }
    }

    /// <summary>
    /// List of candidates for the match
    /// </summary>
    public class ComparisonCandidates : List<IfcRoot> { }

    public enum ComparisonType
    {
        NAME,
        GUID,
        GEOMETRY,
        MATERIAL,
        TYPE,
        ELEMENT_TYPE,
        PROPERTIES,
        CUSTOM
    }

    public enum ResultType
    {
        MATCH,
        ONLY_BASELINE,
        ONLY_REVISION,
        AMBIGUOUS
    }

}
