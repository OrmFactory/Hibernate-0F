using System.Reflection;
using System.Xml.Linq;
using PluginInterfaces;
using PluginInterfaces.Structure;

using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RunToTest.DataStructure.xml"))
{
	var doc = XDocument.Load(stream);
	var project = StructureSerializer.FromXml(doc);
	var generator = new HibernateGenerator.ModelGenerator();
	generator.PackageName = "com.example.servingwebcontent";
	generator.Generate(project, new GenerationOptions
	{
		NewLineChars = "\r\n",
		IndentChars = "\t",
		RootPath = "../../../../Output"
	});
}