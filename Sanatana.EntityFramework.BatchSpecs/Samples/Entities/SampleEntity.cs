using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sanatana.EntityFramework.Batch.Commands.Tests.Samples.Entities
{
    public class SampleEntity
    {
        public int Id { get; set; }
        public int IntNotNullProperty { get; set; }
        public int? IntProperty { get; set; }
        public string StringProperty { get; set; }
        public DateTime? DateProperty { get; set; }
        public Guid GuidProperty { get; set; }
        public Guid? GuidNullableProperty { get; set; }
        public string XmlProperty
        {
            get
            {
                var values = new Dictionary < string, string> {
                    { "key1", "value1" },
                    { "key2", "value2" }
                };
                var xList = values.Select(
                    x => new XElement("item"
                    , new XAttribute("key", x.Key)
                    , new XAttribute("value", x.Value ?? "")));
                var xElem = new XElement("items", xList);
                return xElem.ToString();
            }
            set
            {

            }
        }
    }
}
