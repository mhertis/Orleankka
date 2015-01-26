﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using Orleankka.Dynamic;

namespace Orleankka
{
    public partial class ActorSystem
    {
        public static class Dynamic
        {
            static Dynamic()
            {
                Activator = path => (DynamicActor) System.Activator.CreateInstance(path.Type);

                Serializer = obj =>
                {
                    using (var ms = new MemoryStream())
                    {
                        new BinaryFormatter().Serialize(ms, obj);
                        return ms.ToArray();
                    }
                };

                Deserializer = bytes =>
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        var formatter = new BinaryFormatter();
                        return formatter.Deserialize(ms);
                    }
                };
            }

            /// <summary>
            /// The activation function, which creates actual instances of <see cref="DynamicActor"/>
            /// </summary>
            public static Func<ActorPath, DynamicActor> Activator
            {
                get; set;
            }

            /// <summary>
            /// The serialization function, which serializes messages to byte[]
            /// </summary>
            public static Func<object, byte[]> Serializer
            {
                get { return DynamicMessage.Serializer; }
                set { DynamicMessage.Serializer = value; }
            }

            /// <summary>
            /// The deserialization function, which deserializes byte[] back to messages
            /// </summary>
            public static Func<byte[], object> Deserializer
            {
                get { return DynamicMessage.Deserializer; }
                set { DynamicMessage.Deserializer = value; }
            }

            public static class ActorType
            {
                static readonly string[] separator = {"::"};

                static readonly ConcurrentDictionary<string, Type> cache =
                    new ConcurrentDictionary<string, Type>();

                /// <summary>
                /// The serialization function, which serializes <see cref="Orleankka.ActorPath"/> to runtime identity string
                /// </summary>
                public static Func<ActorPath, string> Serializer = path =>
                    string.Format("{0}{1}{2}", path.Type.FullName, separator[0], path.Id);

                /// <summary>
                /// The deserialization function, which deserializes runtime identity string back to <see cref="Orleankka.ActorPath"/>
                /// </summary>
                public static Func<string, ActorPath> Deserializer = path =>
                {
                    var parts = path.Split(separator, 2, StringSplitOptions.None);
                    return new ActorPath(Find(parts[0]), parts[1]);
                };

                static Type Find(string fullName)
                {
                    return cache.GetOrAdd(fullName, n =>
                    {
                        var candidates = AppDomain.CurrentDomain
                                                  .GetAssemblies()
                                                  .SelectMany(x => x.GetTypes())
                                                  .Where(x => x.FullName == n)
                                                  .ToArray();

                        if (candidates.Length > 1)
                            throw new InvalidOperationException("Multiple types match the given type full name: " + n);

                        if (candidates.Length == 0)
                            throw new InvalidOperationException("Can't find type its by full name: " + n);

                        return candidates[0];
                    });
                }
            }
        }
    }
}