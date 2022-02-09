using System;
using System.Xml.Linq;

namespace Femyou
{
	class Variable : IVariable
	{
		public Variable(XElement xElement)
		{
			Name = xElement.Attribute("name").Value;
			Description = xElement.Attribute("description")?.Value;
			ValueReference = uint.Parse(xElement.Attribute("valueReference").Value);
			Causality = xElement.Attribute("causality")?.Value;
			variability = xElement.Attribute("variability")?.Value;
			Initial = xElement.Attribute("initial")?.Value;
			VariableType = ((System.Xml.Linq.XElement)xElement.FirstNode)?.Name.LocalName;
		}

		public string Name { get; }
		public string Description { get; }
		public uint ValueReference { get; }
		public string Causality { get; }
		public string variability { get; }
		public string Initial { get; }
		public string VariableType { get; }

		/*
        fmi2VariableTypes getfmi2VariableTypes(string fmi2VariableTypeString)
		{
			switch (fmi2VariableTypeString)
			{
				case "Real":
					return fmi2VariableTypes.fmi2Real;
				default:
					throw new NotImplementedException();
			}
		}
		*/
	}

}