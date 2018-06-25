using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using System.Xml;
using System.Windows.Forms;

namespace ggArchicadClassification
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			// Tested with dll from IfcKit from https://github.com/buildingSMART/IfcDoc/tree/master/IfcKit
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Dynamic Link Library (*.dll)|*.dll;";
			if (dialog.ShowDialog() != DialogResult.OK)
				return;

			Assembly assembly = Assembly.LoadFrom(dialog.FileName);
			Type elementType = getTypeByName(assembly, "IfcElement");

			Dictionary<string,ClassificationItem> domains = new Dictionary<string, ClassificationItem>();
			foreach (Type t in assembly.GetTypes())
			{
				if (t.IsAbstract)
					continue;
				if (t.IsSubclassOf(elementType))
				{
					if (t.Name.EndsWith("StandardCase"))
						continue;
					string[] fields = t.Namespace.Split(".".ToCharArray());
					string domain = fields[fields.Length - 1];
					ClassificationItem item = null;
					if (!domains.TryGetValue(domain, out item))
					{
						item = domains[domain] = new ClassificationItem() { ID = domain.ToUpper(), Availability = "ModelElement" };
					}
					ClassificationItem ci = new ClassificationItem() { ID = t.Name, Availability = "ModelElement" };
					string enumName = t.Namespace + "." + t.Name + "TypeEnum";
					Type tenum = assembly.GetType(enumName, false, true);
					if (tenum != null)
					{
						foreach (object o in Enum.GetValues(tenum))
								ci.Children.Add(new ClassificationItem() { ID = t.Name + " " + o.ToString(), Availability = "ModelElement" });
					}
					item.Children.Add(ci);
				}
			}

			XmlDocument doc = new XmlDocument();
			XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
			doc.AppendChild(dec);
			XmlElement element = doc.CreateElement("BuildingInformation");
			doc.AppendChild(element);
			XmlElement classification = doc.CreateElement("Classification");
			element.AppendChild(classification);
			XmlElement system = doc.CreateElement("System");
			classification.AppendChild(system);
			XmlElement name = doc.CreateElement("Name");
			var attributes = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute), false);
			name.InnerText = attributes.Title;
			system.AppendChild(name);
			XmlElement version = doc.CreateElement("EditionVersion");
			version.InnerText = assembly.GetName().Version.ToString(); 
			system.AppendChild(version);
			XmlElement editionDate = doc.CreateElement("EditionDate");
			system.AppendChild(editionDate);
			//XmlElement year = doc.CreateElement("Year");
			//year.InnerText = "1900";
			//editionDate.AppendChild(year);
			//XmlElement month = doc.CreateElement("Month");
			//month.InnerText = "1";
			//editionDate.AppendChild(month);
			//XmlElement day = doc.CreateElement("Day");
			//day.InnerText = "1";
			//editionDate.AppendChild(day);
			XmlElement description = doc.CreateElement("Description");
			system.AppendChild(description);
			XmlElement source = doc.CreateElement("Source");
			var companyAttributes = (AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute), false);
			var productAttributes = (AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false);
			source.InnerText = companyAttributes.Company + " : " + productAttributes.Product;
			system.AppendChild(source);
			XmlElement items = doc.CreateElement("Items");
			system.AppendChild(items);
			foreach (ClassificationItem item in domains.Values)
			{
				XmlElement e = item.getElement(doc);
				items.AppendChild(e);
			}

			XmlElement propertyDefinitionGroups = doc.CreateElement("PropertyDefinitionGroups");
			element.AppendChild(propertyDefinitionGroups);
			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.Filter = "XML (*.xml)|*.xml;";
			if (saveDialog.ShowDialog() != DialogResult.OK)
				return;
			XmlTextWriter writer;
			try
			{
				writer = new XmlTextWriter(saveDialog.FileName, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				doc.WriteTo(writer);
				writer.Flush();
				writer.Close();
			}
			catch(Exception) { }
		}
		public static Type getTypeByName(Assembly assembly, string className)
		{
			foreach (Type t in assembly.GetTypes())
			{
				if (string.Compare( t.Name, className, true) == 0)
					return t;
			}
			return null;
		}
	}

	internal class ClassificationItem
	{
		internal string ID { get; set; } = "";
		internal string Availability { get; set; } = "";
		internal List<ClassificationItem> Children = new List<ClassificationItem>();

		internal XmlElement getElement(XmlDocument doc)
		{
			XmlElement result = doc.CreateElement("Item");
			XmlElement id = doc.CreateElement("ID");
			id.InnerText = ID;
			result.AppendChild(id);
			XmlElement name = doc.CreateElement("Name");
			result.AppendChild(name);
			XmlElement description = doc.CreateElement("Description");
			result.AppendChild(description);
			XmlElement availability = doc.CreateElement("Availability");
			availability.InnerText = Availability;
			result.AppendChild(availability);
			XmlElement children = doc.CreateElement("Children");
			result.AppendChild(children);
			foreach(ClassificationItem i in Children)
			{
				XmlElement element = i.getElement(doc);
				children.AppendChild(element);
			}
			return result;
		}
	}
}
