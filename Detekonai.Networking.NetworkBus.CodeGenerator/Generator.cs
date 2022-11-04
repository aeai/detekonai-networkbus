using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Detekonai.Networking.CodeGenerator
{

    class SerializerFinder : ISyntaxContextReceiver
   //class SerializerFinder : ISyntaxReceiver
	{
		public Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> Serializables { get; } = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
            {
                var classDeclarationSemantics = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                if (classDeclarationSemantics != null)
                {
                    List<INamedTypeSymbol> inheritanceList = new List<INamedTypeSymbol>();
                    INamedTypeSymbol? current = classDeclarationSemantics;
                    while (current != null)
                    {
                        inheritanceList.Add(current);
                        current = current.BaseType;
                    }
                    while (inheritanceList.Count > 0)
                    {
                        if (!inheritanceList.Last().GetAttributes().Any(x => x.AttributeClass.ToDisplayString() == "Detekonai.Networking.NetworkSerializableAttribute"))
                        {
                            inheritanceList.Remove(inheritanceList.Last());
                        }
                        else
                        {
                            if (!classDeclarationSemantics.IsAbstract) 
							{
									Serializables[classDeclarationSemantics] = inheritanceList;
							}
                            break;
                        }
                    }
                }
            }
        }
    }

    [Generator]
	public class NetworkSerializerGenerator : ISourceGenerator
	{
		private static Dictionary<string, Func<string, string>> serializerMap = new Dictionary<string, Func<string, string>> (){
			{ "string", (string s) => $"blob.AddString({s});" },
			{ "System.String", (string s) => $"blob.AddString({s});" },
			{ "int", (string s) => $"blob.AddInt({s});" },
			{ "System.Int32", (string s) => $"blob.AddInt({s});" },
			{ "uint", (string s) => $"blob.AddUInt({s});" },
			{ "long", (string s) => $"blob.AddLong({s});" },
			{ "ulong", (string s) => $"blob.AddULong({s});" },
			{ "byte", (string s) => $"blob.AddByte({s});" },
			{ "short", (string s) => $"blob.AddShort({s});" },
			{ "ushort", (string s) => $"blob.AddUShort({s});" },
			{ "float", (string s) => $"blob.AddSingle({s});" },
			{ "System.TimeSpan", (string s) => $"blob.AddLong({s}.Ticks);" },
			{ "System.DateTimeOffset", (string s) => $"blob.AddLong({s}.UtcTicks);" },
			{ "object", (string s) => $@"{{
						if({s} == null)
						{{
							blob.AddUInt(0);
						}}
						else
						{{
							INetworkSerializer oser = owner.Factory.Get({s}.GetType());
							if(oser != null)
							{{
								blob.AddUInt(oser.ObjectId);
								oser.Serialize(blob, {s});
							}}
							else
							{{
								var act = ObjectPrimitiveSerializerHelper.GetObjectSerializer({s}.GetType());
								if(act != null)
								{{
									act(blob, {s});
								}}
								else
								{{
									Console.WriteLine($""Serialization failed, Missing type: {{{s}.GetType()}}"");
									blob.AddUInt(0);
								}}
							}}
						}}
			}}" }
		};
		private static Dictionary<string, Func<string, string>> deserializerMap = new Dictionary<string, Func<string, string>>(){
			{ "string", (string s) => $"{s} = blob.ReadString();" },
			{ "System.String", (string s) => $"{s} = blob.ReadString();" },
			{ "int", (string s) => $"{s} = blob.ReadInt();" },
			{ "System.Int32", (string s) => $"{s} = blob.ReadInt();" },
			{ "uint", (string s) => $"{s} = blob.ReadUInt();" },
			{ "long", (string s) => $"{s} = blob.ReadLong();" },
			{ "ulong", (string s) => $"{s} = blob.ReadULong();" },
			{ "byte", (string s) => $"{s} = blob.ReadByte();" },
			{ "short", (string s) => $"{s} = blob.ReadShort();" },
			{ "ushort", (string s) => $"{s} = blob.ReadUShort();" },
			{ "float", (string s) => $"{s} = blob.ReadSingle();" },
			{ "System.TimeSpan", (string s) => $"{s} = TimeSpan.FromTicks(blob.ReadLong());" },
			{ "System.DateTimeOffset", (string s) => $"{s} = new DateTimeOffset(blob.ReadLong(), TimeSpan.Zero);" },
			{ "object", (string s) => $@"
										{{
											uint id = blob.ReadUInt();
											var ser = owner.Factory.Get(id);
											if(ser != null)
											{{
												{s} = ser.Deserialize(blob);
											}}
											else
											{{
												var act = ObjectPrimitiveSerializerHelper.GetObjectDeserializer(id);
												if(act != null)
												{{
													{s} = act(blob);
												}}
												else
												{{
													{s} = null;
												}}
											}}
										}}
										" }
		};

		private uint GenerateId(string name) 
		{
			byte[] data = new byte[1024];
			uint len = (uint)System.Text.Encoding.UTF8.GetBytes(name, 0, name.Length, data, 0);
			return MurmurHash3.Hash(data, len, 19850922);
		}

		private bool IsVirtual(ISymbol symbol)
        {
			AttributeData? ad = symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass!.ToDisplayString() == "Detekonai.Networking.NetworkSerializablePropertyAttribute");
			if (ad != null)
			{
				var keyValue = ad.NamedArguments.FirstOrDefault(x => x.Key == "Virtual");
				if(keyValue.Value.Value != null)
                {
					return (bool)keyValue.Value.Value;
                }
			}
			return false;
		}
		private void AddSerializerClass(StringBuilder sb, List<INamedTypeSymbol> inheritanceList, SerializerFinder finder) 
		{
			INamedTypeSymbol target = inheritanceList.First();
			string fullName = target.ToDisplayString();

			sb.Append($@"
				private class {target.Name}Serializer : INetworkSerializer
				{{
							public uint ObjectId {{ get; }} = {GenerateId(fullName)};
							public Type SerializedType {{ get; }} = typeof({fullName});
							public int RequiredSize {{ get; }} = 0;
							private INetworkSerializerFactoryProvider owner;
							public {target.Name}Serializer(INetworkSerializerFactoryProvider factory)
							{{
								owner = factory;
							}}
			");
			sb.Append($@"
							public object Deserialize(BinaryBlob blob)
							{{
								{fullName} res = new {fullName}();
			");
			foreach (var targetClass in inheritanceList)
			{
				foreach (var symbol in targetClass.GetMembers().Where(x => x.Kind == SymbolKind.Property && x.GetAttributes().Any(y => y.AttributeClass!.ToDisplayString() == "Detekonai.Networking.NetworkSerializablePropertyAttribute")))
				{
					var prop = (IPropertySymbol)symbol;
	
					DeserializeProperty(sb, prop, finder);
				}
			}
			sb.Append($@"	
								return res;
							}}
							public void Serialize(BinaryBlob blob, object ob)
							{{
								var exo = ({fullName})ob;
			");
			foreach (var targetClass in inheritanceList)
			{
				foreach (var symbol in targetClass.GetMembers().Where(x => x.Kind == SymbolKind.Property && x.GetAttributes().Any(y => y.AttributeClass!.ToDisplayString() == "Detekonai.Networking.NetworkSerializablePropertyAttribute")))
				{
					var prop = (IPropertySymbol)symbol;
					SerializeProperty(sb, prop, finder);
				}
			}
			sb.Append("}}");
		}
		private bool SerializeProperty(StringBuilder sb, ITypeSymbol type, string name, SerializerFinder finder, bool virt)
		{
            var collection = type.AllInterfaces.Where(x => x.ContainingSymbol?.ToDisplayString() == "System.Collections.Generic" && x.Name == "ICollection").FirstOrDefault();
			if (collection != null)
			{
				if (collection.TypeArguments.First().ContainingSymbol.ToDisplayString() == "System.Collections.Generic" && collection.TypeArguments.First().Name == "KeyValuePair")
				{
					sb.Append($@"	
										if({name} == null)
										{{
											blob.AddUShort(0);
										}}
										else
										{{
											blob.AddUShort((ushort)({name}.Count + 1));
											foreach (var d in {name})
											{{
							");
					INamedTypeSymbol? keyValuePair = collection.TypeArguments.First() as INamedTypeSymbol;
					ITypeSymbol key = keyValuePair.TypeArguments.First();
					ITypeSymbol value = keyValuePair.TypeArguments.Last();
					SerializeProperty(sb, key, "d.Key", finder, virt);
					SerializeProperty(sb, value, "d.Value", finder, virt);
					sb.Append($@"
											}}
										}}
							");
				}
				else
				{
					sb.Append($@"	
										if({name} == null)
										{{
											blob.AddUShort(0);
										}}
										else
										{{
											blob.AddUShort((ushort)((({collection.ToDisplayString()}){name}).Count + 1));
											foreach (var d in {name})
											{{
							");

					ITypeSymbol key = collection.TypeArguments.First();
					SerializeProperty(sb, key, "d", finder, virt);
					sb.Append($@"
											}}
										}}
							");
				}
				return true;
				//enumerator.TypeArguments
			}
			else if (serializerMap.TryGetValue(type.ToString(), out Func<string, string> val))
			{
				sb.AppendLine(val(name));
				return true;
			}
			else if (type is INamedTypeSymbol named)
			{
				if (virt)
				{
					sb.AppendLine($@"
						{{
							INetworkSerializer ser;
							if({name} == null)
							{{
								ser = owner.Factory.Get(typeof({named.ToDisplayString()}));
							}}
							else
							{{
								ser = owner.Factory.Get({name}.GetType());
							}}
							if(ser != null)
							{{
								blob.AddUInt(ser.ObjectId);
								ser.Serialize(blob, {name});
							}}
						}}
					");
				}
				else
				{
					sb.AppendLine($@"
							{{
							INetworkSerializer ser = owner.Factory.Get(typeof({named.ToDisplayString()}));
							if(ser != null)
							{{
								ser.Serialize(blob, {name});
							}}
							}}
					");
				}

				return true;
			}
			else
			{
				sb.AppendLine($"//Missing: {type.ToDisplayString()} {name}");
			}
			return false;
		}
		private bool SerializeProperty(StringBuilder sb, IPropertySymbol prop, SerializerFinder finder) 
		{
			return SerializeProperty(sb, prop.Type, $"exo.{prop.Name}", finder, IsVirtual(prop));
		}
		private bool DeserializeProperty(StringBuilder sb, IPropertySymbol prop, SerializerFinder finder)
		{
			return DeserializeProperty(sb, prop.Type, $"res.{prop.Name}", finder, IsVirtual(prop));
		}
		private bool  DeserializeProperty(StringBuilder sb, ITypeSymbol type, string name, SerializerFinder finder, bool virt)
		{
            var collection = type.AllInterfaces.Where(x => x.ContainingSymbol?.ToDisplayString() == "System.Collections.Generic" && x.Name == "ICollection").FirstOrDefault();
            if (collection != null)
            {
                if (collection.TypeArguments.First().ContainingSymbol.ToDisplayString() == "System.Collections.Generic" && collection.TypeArguments.First().Name == "KeyValuePair")
                {
                    sb.Append($@"	
								{{
									ushort count = blob.ReadUShort();
									if (count == 0)
									{{
										{name} = null;
									}}
									else
									{{
										count--;
										{name} = new {type.ToDisplayString()}();
										for (int i = 0; i < count; i++)
										{{
								");
					INamedTypeSymbol? keyValuePair = collection.TypeArguments.First() as INamedTypeSymbol;
					ITypeSymbol key = keyValuePair.TypeArguments.First();
					ITypeSymbol value = keyValuePair.TypeArguments.Last();
					sb.AppendLine($"{key.ToDisplayString()} key = default;");
					sb.AppendLine($"{value.ToDisplayString()} value = default;");
					bool keyFound = DeserializeProperty(sb, key, "key", finder, virt);
                    bool valueFound = DeserializeProperty(sb, value, "value", finder, virt);
                    if(keyFound && valueFound)
					{
						sb.Append($@"{name}[key] = value;");
					}
					sb.Append($@"
											
										}}
									}}
								}}
							");
                }
                else
                {
                    sb.Append($@"	
								{{
								ushort count = blob.ReadUShort();
								if (count == 0)
								{{
									{name} = null;
								}}
								else
								{{
									count--;
									
					");
					if(type.Kind == SymbolKind.ArrayType)
                    {
						sb.Append($@"{name} = new {type.ToDisplayString().Substring(0, type.ToDisplayString().Length-2)}[count];");
                    }
                    else
					{
						sb.Append($@"{name} = new {type.ToDisplayString()}();");
					}
					sb.Append($@"
									for (int i = 0; i < count; i++)
									{{
							");
                    ITypeSymbol value = collection.TypeArguments.First();
					sb.AppendLine($"{value.ToDisplayString()} value = default;");
					bool valueFound = DeserializeProperty(sb, value, $"value", finder, virt);
					if (valueFound)
					{
						if (type.Kind == SymbolKind.ArrayType)
						{
							sb.Append($@"{name}[i] = value;");
						}
						else
						{
							sb.Append($@"{name}.Add(value);");
						}
						
					}
					sb.Append($@"
										
									}}
								}}
}}
							");
                }
				return true;
            }
            if (deserializerMap.TryGetValue(type.ToString(), out Func<string, string> val))
			{
				sb.AppendLine(val(name));
				return true;
			}
			if (type is INamedTypeSymbol named && finder.Serializables.TryGetValue(named, out List<INamedTypeSymbol> list))
			{
				//todo exception or something if ser is not found
				if (virt)
				{
					sb.AppendLine($"{name} = ({named.ToDisplayString()})owner.Factory.Get(blob.ReadUInt()).Deserialize(blob);");
				}
				else
				{
					sb.AppendLine($"{name} = ({named.ToDisplayString()})owner.Factory.Get(typeof({named.ToDisplayString()})).Deserialize(blob);");
				}
				return true;
			}
			return false;
		}

		public void Execute(GeneratorExecutionContext context)
		{
			SerializerFinder finder = (SerializerFinder)context!.SyntaxContextReceiver;
			if(finder.Serializables.Count == 0)
            {
				return;
            }
			string name = Regex.Replace(context.Compilation.Assembly.Name, @"[\.\-_]+", "");
			//context.Compilation.AssemblyName
			var sourceBuilder = new StringBuilder(@$"
					using System;
					using Detekonai.Core.Common;
					using Detekonai.Core;
					using Detekonai.Networking.Serializer.Experimental;

					namespace Detekonai.Networking.Serializer.Experimental
			            {{
			                public  class {name}SerializerFactory : AbstractNetworkSerializerFactory, INetworkSerializerFactoryProvider
			                {{
								public INetworkSerializerFactory Factory {{get;set;}}
					public void Dump(){{

			            ");
			sourceBuilder.AppendLine($"//finder count:{finder.Serializables.Count}");
			//var messages = GetSerializbles(context.Compilation);
			//HashSet<string> propertyTypes = new HashSet<string>();
			//go through all network messages
			foreach (var serializable in finder.Serializables)
			{
				sourceBuilder.AppendLine($"Logger.Log(this, \"{serializable.Key}\");");
				foreach (var baseClass in serializable.Value)
				{
					sourceBuilder.AppendLine($"Logger.Log(this, \"------{baseClass.Name}\");");
					foreach (var prop in baseClass.GetMembers().Where(x => x.Kind == SymbolKind.Property && x.GetAttributes().Any(y => y.AttributeClass!.ToDisplayString() == "Detekonai.Networking.NetworkSerializablePropertyAttribute")))
                    {
						var pp = (IPropertySymbol)prop;
							
						sourceBuilder.AppendLine($"Logger.Log(this, \"------------{pp.Type}  {prop.Name} {IsVirtual(pp)}\");");
						AttributeData? ad = pp.GetAttributes().FirstOrDefault(x => x.AttributeClass!.ToDisplayString() == "Detekonai.Networking.NetworkSerializablePropertyAttribute");
						if (ad != null)
						{
							foreach (var na in ad.NamedArguments)
							{
								sourceBuilder.AppendLine($"Logger.Log(this, \"---------------+{na.Key}={na.Value.Value}\");");
							}
						}
						foreach (var iface in pp.Type.AllInterfaces) 
						{
							sourceBuilder.AppendLine($"Logger.Log(this, \"--------------------{iface.ContainingSymbol?.ToDisplayString()}.{iface.Name}\");");
							if(iface.TypeArguments != null)
							{
								foreach (var ta in iface.TypeArguments)
								{
									sourceBuilder.AppendLine($"Logger.Log(this, \"------------------------{ta.ContainingSymbol?.ToDisplayString()} {ta.Name}\");");
								}
							}
						}
					}
				}
			}

			sourceBuilder.AppendLine("}");
			foreach (var serializable in finder.Serializables)
			{
				AddSerializerClass(sourceBuilder, serializable.Value, finder);
			}
			sourceBuilder.AppendLine(@$"
							public {name}SerializerFactory(ILogger logger)
							{{
								Logger = logger;

			");
			foreach (var serializable in finder.Serializables)
			{
				sourceBuilder.AppendLine($"AddSerializer(typeof({serializable.Key.ToDisplayString()}), new {serializable.Key.Name}Serializer(this));");
			}

			sourceBuilder.AppendLine(@"
							}
						}
					}
			");

			//	context.Compilation.
			context.AddSource($"{name}SerializerFactory.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications(() => new SerializerFinder());
		}
	}
}
