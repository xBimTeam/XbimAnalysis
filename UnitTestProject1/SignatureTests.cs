using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.ModelGeometry.Scene;
using XbimGeometry.Interfaces;
using Xbim.IO;
using Xbim.Ifc2x3.ProductExtension;
using System.Diagnostics;

namespace Xbim.Analysis.Tests
{
    [TestClass]
    public class SignatureTests
    {
        [TestMethod]
        public void SimpleIfcElementSignatureTest()
        {
            using (var model = new XbimModel())
            {
                
                model.CreateFrom("Standard Classroom CIC 6.ifc", null,null,true);
                var geomContext = new Xbim3DModelContext(model);
                geomContext.CreateContext(XbimGeometryType.PolyhedronBinary);
                var summary = new IfcElementSignatureSummary();
                foreach (var elem in model.Instances.OfType<IfcElement>())
                {
                    var signature = new IfcElementSignature(elem, geomContext);
                    summary.Add(signature);
                    Debug.WriteLine(signature.ToCSV());
                }
                Debug.WriteLine(summary.ToString());
            }
        }
    }
}
