using System;

namespace Femyou
{
	public interface IVariable
	{
		string Name { get; }
		string Description { get; }
		uint ValueReference { get; }
		string Causality { get; }
		string variability { get; }
		string Initial { get; }
		string VariableType { get; }
	}
}