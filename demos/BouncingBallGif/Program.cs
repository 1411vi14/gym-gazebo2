using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnimatedGif;
using Femyou;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Utf8Json;

namespace Femyou.Demos.BouncingBallGif
{
	class Program
	{
		static void Main()
		{
			/*
			dynamic a = 1;
			a = "abc";
			int inta = (a + 5);
			*/

			object b = 1.0;
			b = "cde";
			Type t = b.GetType();


			FileStream configFileStream = new FileStream(@"config.json", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var configFile = JsonSerializer.Deserialize<dynamic>(configFileStream);
			configFileStream.Close();

			var modellconfigs = configFile["Modelle"];
			int countModelle = modellconfigs.Count;
			Femyou.IModel[] models = new Femyou.IModel[countModelle];
			List<Femyou.IInstance> instances = new List<IInstance>();
			List<ModelVariable> variables1 = new List<ModelVariable>();

			for (int i = 0; i < countModelle; i++)
			{
				var modelcfg = modellconfigs[i];
				int countInstances = modelcfg["instances"].Count;
				var file = modelcfg["fmufile"];
				Femyou.IModel model = Model.Load(file);
				models[i] = model;
				for (int j = 0; j < countInstances; j++)
				{
					var name = modelcfg["instances"][j]["name"];
					instances.Add(Tools.CreateInstance(model, name));
					int countVars = modelcfg["instances"][j]["varmapping"].Count;
					for (int k = 0; k < countVars; k++)
					{
						bool isCausalitySet = modelcfg["instances"][j]["varmapping"][k].TryGetValue("causality", out dynamic causality);
						if (isCausalitySet && (string)causality.ToLower() == "independent")
							continue;
						bool hasInitiialValue = modelcfg["instances"][j]["varmapping"][k].TryGetValue("start", out dynamic val);
						if (val is Array)
                        {
							val = val[0];
                        }
						bool hasVarInID = modelcfg["instances"][j]["varmapping"][k].TryGetValue("varInID", out dynamic varInID);

						variables1.Add(new ModelVariable
						{
							name = modelcfg["instances"][j]["varmapping"][k]["name"],
							varID = (int)modelcfg["instances"][j]["varmapping"][k]["varID"],
							varInID = (int?)varInID,
							type = fmi2Type.fmi2Real,
							model = i,
							instance = j,
							startValue = val
						});
					}
				}
			}

			InitialisizeAllFMUVariables(models, instances, variables1);

			

			//dynamic[] variablenwerte = new dynamic[variables1.Count];
			Dictionary<int, dynamic> variablenwerte = new Dictionary<int, dynamic>();

			//ReadAllFMUVariables(models, instances, variables1, ref variablenwerte);

			//WriteAllFMU2Variables(models, instances, variables1, variablenwerte);

			foreach (var _instance in instances)
			{
				_instance.StartTime(0.0);
			}

			int step = 0;
			do
			{
				ReadAllFMUVariables(models, instances, variables1, ref variablenwerte);
				WriteAllFMU2Variables(models, instances, variables1, variablenwerte);

				foreach (var _instance in instances)
				{
					_instance.AdvanceTime(0.1);
				}
				Console.WriteLine($"Iteration {step}:");
                foreach (var item in variablenwerte)
                {
					Console.WriteLine($"V{item.Key}: {item.Value}\t");
				}
				Console.WriteLine();
				step++;
			} while (step < 20);

		}

		private static void InitialisizeAllFMUVariables(IModel[] models, List<IInstance> instances, List<ModelVariable> variables1)
		{
			//Initialisieren der Variablen
			for (int i = 0; i < variables1.Count; i++)
			{
				Femyou.IModel model = models[variables1[i].model];
				var variable = model.Variables[variables1[i].name];
				var val = variables1[i].startValue;
				if (val == null)
					continue;
				var _instance = instances.ToArray()[variables1[i].instance];
				switch (variables1[i].type)
				{
					case fmi2Type.fmi2Real:
						_instance.WriteReal((variable, val));
						break;
					case fmi2Type.fmi2Integer:
						_instance.WriteInteger((variable, val));
						break;
					case fmi2Type.fmi2Boolean:
						_instance.WriteBoolean((variable, val));
						break;
					case fmi2Type.fmi2String:
						_instance.WriteString((variable, val));
						break;
					default:
						break;
				}

			}
		}

		static void WriteAllFMU2Variables(IModel[] models, List<IInstance> instances, List<ModelVariable> variables1, Dictionary<int,dynamic> variablenwerte)
		{
			//Austausch der Variablene als Byteblock
			//Schreiben aller Variablen
			for (int i = 0; i < variables1.Count; i++)
			{
				if (variables1[i].varInID == null)
					continue;
				Femyou.IModel model = models[variables1[i].model];
				var variable = model.Variables[variables1[i].name];
				dynamic val = variablenwerte[(int)variables1[i].varInID];
				var _instance = instances.ToArray()[variables1[i].instance];
				switch (variables1[i].type)
				{
					case fmi2Type.fmi2Real:
						_instance.WriteReal((variable, val));
						break;
					case fmi2Type.fmi2Integer:
						_instance.WriteInteger((variable, val));
						break;
					case fmi2Type.fmi2Boolean:
						_instance.WriteBoolean((variable, val));
						break;
					case fmi2Type.fmi2String:
						_instance.WriteString((variable, val));
						break;
					default:
						break;
				}
			}
		}

		private static void ReadAllFMUVariables(IModel[] models, List<IInstance> instances, List<ModelVariable> variables1, ref Dictionary<int,dynamic> variablenwerte)
		{
			//Lesen aller Variablen
			for (int i = 0; i < variables1.Count; i++)
			{
				Femyou.IModel model = models[variables1[i].model];
				var variable = model.Variables[variables1[i].name];
				dynamic val = null;
				var _instance = instances[variables1[i].instance];
				switch (variables1[i].type)
				{
					case fmi2Type.fmi2Real:
						val = _instance.ReadReal((variable));
						break;
					case fmi2Type.fmi2Integer:
						val = _instance.ReadInteger((variable));
						break;
					case fmi2Type.fmi2Boolean:
						val = _instance.ReadBoolean((variable));
						break;
					case fmi2Type.fmi2String:
						val = _instance.ReadString((variable));
						break;
					default:
						break;
				}
				if (val is Array)
				{
					val = val[0];
				}
				variablenwerte[variables1[i].varID] = val;
			}
		}

	}

	class ModelVariable
	{
		public string name;
		public int varID;
		public int? varInID;
		public fmi2Type type;
		public int model;
		public dynamic? startValue;
		public int instance;
	}
	/*
	class ModelVariableReal : IModelVariable
	{
		public new double value;
		
	}
	class ModelVariableInteger : IModelVariable
	{
		public new int value;
	}
	class ModelVariableBoolean : IModelVariable
	{
		public new bool value;
	}
	class ModelVariableString : IModelVariable
	{
		public new string value;
	}
	*/


	public enum fmi2Type
	{
		fmi2Real,
		fmi2Integer,
		fmi2Boolean,
		fmi2String
	}
}
