using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;

namespace Xbim.Analysis.Extensions
{
    public static class IfcSimpleValueExtensions
    {
        /// <summary>
        /// Extension method to create hash of the IfcSimpleValue
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int CreateHash(this IIfcSimpleValue value)
        {
            if (value == null)
                throw new ArgumentNullException();

            //hash of the type
            var typeName = value.GetType().Name;
            var typeHash = typeName.GetHashCode();
            //switch(typeName)
            //{
            //    case "IfcInteger":
            //        typeHash = 
            //        break;
            //    case "IfcReal":
            //        break;
            //    case "IfcBoolean":
            //        break;
            //    case "IfcIdentifier":
            //        break;
            //    case "IfcText":
            //        break;
            //    case "IfcLabel":
            //        break;
            //    case "IfcLogical":
            //        break;
            //    default:
            //        break;
            //}
             	

            //hash of the actual value
            var valHash = value.ToString().GetHashCode();

            return valHash + typeHash;
        }
    }
}
