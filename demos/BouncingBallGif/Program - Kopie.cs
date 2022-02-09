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
						bool hasVarInID = modelcfg["instances"][j]["varmapping"][k].TryGetValue("start", out dynamic varInID);

						variables1.Add(new ModelVariable
						{
							name = modelcfg["instances"][j]["varmapping"][k]["name"],
							varID = (int)modelcfg["instances"][j]["varmapping"][k]["varID"],
							varInID = varInID,
							type = fmi2Type.fmi2Real,
							model = i,
							instance = j,
							startValue = val
						});
					}
				}
			}

			InitialisizeAllFMUVariables(models, instances, variables1);

			dynamic[] variablenwerte = new dynamic[variables1.Count];

			//ReadAllFMUVariables(models, instances, variables1, ref variablenwerte);

			//WriteAllFMU2Variables(models, instances, variables1, variablenwerte);

			var v = 0.0;
			double v2 = 0f;
			double h = 10, h2;

			//using var model = Model.Load(Path.Combine(fmuFolder, "BouncingBall.fmu"));
			using var model1 = Model.Load(@"S:\David\Master\Reference-FMUs\build\dist\BouncingBall.fmu");

			using var instance = Tools.CreateInstance(model1, "demo1");
			using var instance2 = Tools.CreateInstance(model1, "demo2");

			var altitude = model1.Variables["h"];
			var velocity = model1.Variables["v"];
			instance.WriteReal((altitude, 5));
			instance.WriteReal((velocity, v));
			instance2.WriteReal((altitude, 10));
			instance2.WriteReal((velocity, v));

			var variablesX1 = instance.ReadReal(altitude, velocity);
			var variablesX2 = instance2.ReadReal(altitude, velocity);
			//using var gif = new AnimatedGifCreator("BouncingBall.gif");

			instance.StartTime(0.0);
			instance2.StartTime(0.0);

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
				for (int i = 0; i < variablenwerte.Length; i++)
				{
					Console.Write($"V{i}: {variablenwerte[i]}\t");
				}
				Console.WriteLine();
				step++;
			} while (step < 20);

			
			bool runde = true;
			while (h > 0 || Math.Abs(v) > 0)
			{
				var variables = instance.ReadReal(altitude, velocity);
				var variables2 = instance2.ReadReal(altitude, velocity);
				h = variables.First();
				v = variables.Last();
				h2 = variables2.First();
				v2 = variables2.Last();
				//Console.WriteLine($"h: {h} v: {v} h2: {h2} v2: {v2}");

				Console.Write("Instanz 1: ");
				Console.ForegroundColor = runde ? ConsoleColor.Green : ConsoleColor.Red;
				Console.WriteLine($"h: {h} v: {v}");
				Console.ResetColor();
				Console.Write("Instanz 2: ");
				Console.ForegroundColor = !runde ? ConsoleColor.Green : ConsoleColor.Red;
				Console.WriteLine($"h: {h2} v: {v2}");
				Console.ResetColor();
				/*AddFrame(gif, info, canvas =>
				{
				  canvas.Clear(SKColors.WhiteSmoke);
				  canvas.DrawText($"h = {h:F2} m", hCoord, paint);
				  canvas.DrawText($"v = {v:F2} m/s", vCoord, paint);
				  canvas.DrawCircle(info.Width / 2, info.Height - (int)h - r, r, paint);
				});*/
				instance2.WriteReal((altitude, h));
				instance2.WriteReal((velocity, v));
				instance.WriteReal((altitude, h2));
				instance.WriteReal((velocity, v2));

				Task t1 = Task.Run(() => instance.AdvanceTime(0.1));
				Task t2 = Task.Run(() => instance2.AdvanceTime(0.1));

				instance.AdvanceTime(0.1);
				instance2.AdvanceTime(0.1);
				runde = !runde;
			}

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

		static void WriteAllFMU2Variables(IModel[] models, List<IInstance> instances, List<ModelVariable> variables1, dynamic[] variablenwerte)
		{
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

		private static void ReadAllFMUVariables(IModel[] models, List<IInstance> instances, List<ModelVariable> variables1, ref dynamic[] variablenwerte)
		{
			//Lesen aller Variablen
			for (int i = 0; i < variables1.Count; i++)
			{
				Femyou.IModel model = models[variables1[i].model];
				var variable = model.Variables[variables1[i].name];
				dynamic val = null;
				var _instance = instances.ToArray()[variables1[i].instance];
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
				variablenwerte[variables1[i].varID] = val;
			}
		}

		static readonly string fmuFolder = Path.Combine(Tools.GetBaseFolder(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).AbsolutePath, nameof(Femyou)), "FMU", "bin", "dist");

		private static void AddFrame(AnimatedGifCreator gif, SKImageInfo info, Action<SKCanvas> action)
		{
			using var surface = SKSurface.Create(info);
			action(surface.Canvas);
			using var image = surface.Snapshot();
			using var bitmap = image.ToBitmap();
			gif.AddFrame(bitmap, quality: GifQuality.Bit8);
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
