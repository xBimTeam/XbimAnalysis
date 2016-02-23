using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Analysis.Tests
{
    [TestClass]
    public class SignatureTests
    {
        [TestMethod]
        public void SimpleIfcElementSignatureTest()
        {
            using (var model = IfcStore.Open("Standard Classroom CIC 6.ifc"))
            {

                var geomContext = new Xbim3DModelContext(model);
                geomContext.CreateContext();
                //var summary = new IfcElementSignatureSummary();
                //foreach (var elem in model.Instances.OfType<IfcElement>())
                //{
                //    var signature = new IfcElementSignature(elem, geomContext);
                //    summary.Add(signature);
                //    Debug.WriteLine(signature.ToCSV());
                //}
                //Debug.WriteLine(summary.ToString());
            }
        }
    }
}
