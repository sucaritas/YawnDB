// <copyright file="Utilities.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Bond;
    using Bond.IO.Unsafe;
    using Bond.Protocols;
    using YawnDB.Interfaces;

    public static class Utilities
    {
        public static IDictionary<string, IIndex> GetIndeciesFromSchema(Type schemaType, Type storageLocationType)
        {
            IDictionary<string, IIndex> indicies = new Dictionary<string, IIndex>();
            var bondSchema = Schema.GetRuntimeSchema(schemaType);
            var schemaDefFieldMetadata = bondSchema.SchemaDef.structs
                                            .SelectMany(x => x.fields)
                                            .Where(x => x.metadata.attributes.ContainsKey("Index")).Select(x => x.metadata);

            Dictionary<string, List<Tuple<Type, PropertyInfo, int>>> indeciesInfo = new Dictionary<string, List<Tuple<Type, PropertyInfo, int>>>();
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var fieldMetadata in schemaDefFieldMetadata)
            {
                foreach (var attribute in fieldMetadata.attributes)
                {
                    if (attribute.Key.Equals("Index", StringComparison.Ordinal))
                    {
                        var attributeParts = attribute.Value.Replace(" ", string.Empty).Split(',');
                        List<Tuple<Type, PropertyInfo, int>> list;
                        if (!indeciesInfo.TryGetValue(attributeParts[1], out list))
                        {
                            list = new List<Tuple<Type, PropertyInfo, int>>();
                        }

                        list.Add(new Tuple<Type, PropertyInfo, int>(
                                                                    assembly.GetType(attributeParts[0]),
                                                                    schemaType.GetRuntimeProperty(fieldMetadata.name),
                                                                    int.Parse(attributeParts[2])));
                        indeciesInfo[attributeParts[1]] = list;
                    }
                }
            }

            foreach (var indexInfo in indeciesInfo)
            {
                IIndex index = Activator.CreateInstance(indexInfo.Value[0].Item1) as IIndex;
                index.Name = indexInfo.Key;
                index.StorageLocationType = storageLocationType;
                foreach (var info in indexInfo.Value.OrderBy(x => x.Item3))
                {
                    index.IndexParameters.Add(new IndexParameter() { Name = info.Item2.Name, ParameterGetter = info.Item2 });
                }

                indicies[indexInfo.Key] = index;
            }

            return indicies;
        }
    }
}
