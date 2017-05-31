using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.ModelGeometry.Scene;
using Xbim.IO;
using System.Diagnostics;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4;
using Xbim.Ifc4.Interfaces;
using Xbim.Common.Geometry;

namespace Xbim.Analysis.Tests
{
    [TestClass]
    public class SignatureTests
    {
        [TestMethod]
        public void SimpleIfcElementSignatureTest()
        {
            using (var model = new IO.Esent.EsentModel(new EntityFactory()))
            {               
                model.CreateFrom("Standard Classroom CIC 6.ifc", null,null,true);

                using (var geomReader = model.GeometryStore.BeginRead())
                {

                    var summary = new IfcElementSignatureSummary();
                    foreach (var elem in model.Instances.OfType<IIfcElement>())
                    {
                        var signature = new IfcElementSignature(elem, geomReader);
                        summary.Add(signature);
                        Debug.WriteLine(signature.ToCSV());
                    }
                    Debug.WriteLine(summary.ToString());
                }
            }
        }
    }
}
