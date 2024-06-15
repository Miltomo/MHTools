using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace MHTools
{
    /// <summary>
    /// 数据处理帮助工具集
    /// </summary>
    public static class DataHelper
    {
        private static readonly BindingFlags ALLKINDS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly JsonSerializerOptions _Options_JSON_ = new()
        {
            IncludeFields = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
        };

        /// <summary>
        /// 标记一个类，其中所有变量均为存储目标「MHTools.DataHelper」
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class SaveAllAttribute : Attribute { }

        /// <summary>
        /// 标记一个类，忽略特定类型的变量，剩余所有变量均为存储目标「MHTools.DataHelper」
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class SaveAllWithoutAttribute(BindingFlags ignoredFlags) : Attribute
        {
            public BindingFlags IgnoredFlags => ignoredFlags;
        }

        /// <summary>
        /// 标记一个成员变量为数据存储目标「MHTools.DataHelper」
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public class ToSaveAttribute : Attribute { }

        /// <summary>
        /// 标记一个成员变量为非存储目标。(仅当SaveAll时生效)「MHTools.DataHelper」
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
        public class DoNotSaveAttribute : Attribute { }

        /// <summary>
        /// 获取目标类型的所有被标记为"数据存储对象"的成员名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string[] GetToSaveMemberName(Type type)
        {
            IEnumerable<MemberInfo> orins;

            if (type.IsDefined(typeof(SaveAllAttribute), false))
                orins = type.GetMembers(ALLKINDS)
                    .Where(member => (member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property) && !member.IsDefined(typeof(DoNotSaveAttribute), false))
                    .Where(m => !m.Name.Contains('<'));
            else if (type.IsDefined(typeof(SaveAllWithoutAttribute), false))
            {
                var attribute = type.GetCustomAttribute<SaveAllWithoutAttribute>();
                orins = type.GetMembers(ALLKINDS ^ attribute!.IgnoredFlags)
                    .Where(member => (member.MemberType == MemberTypes.Field || member.MemberType == MemberTypes.Property) && !member.IsDefined(typeof(DoNotSaveAttribute), false))
                    .Where(m => !m.Name.Contains('<'));
            }
            else
            {
                orins =
                    type.GetMembers(ALLKINDS)
                   .Where(member => member.IsDefined(typeof(ToSaveAttribute), false));
            }

            return orins
                   .Select(member => member.Name)
                   .ToArray();
        }

        /// <summary>
        ///  (反射) 强制获取目标成员变量
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        public static object? ReadValue(object obj, string memberName)
        {
            Type? type;
            object? target = default;
            try
            {
                type = (Type)obj;
            }
            catch (Exception)
            {
                type = obj.GetType();
                target = obj;
            }

            return ReadValue(type, memberName, instance: target);
        }

        /// <summary>
        /// (反射) 强制读值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberName"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static object? ReadValue(Type type, string memberName, object? instance = default)
        {
            MemberInfo[] members = type.GetMember(memberName, ALLKINDS);

            if (members.Length > 0)
            {
                MemberInfo member = members[0];

                if (member is FieldInfo fieldInfo)
                {
                    return fieldInfo.GetValue(instance);
                }
                else if (member is PropertyInfo propertyInfo)
                {
                    return propertyInfo.GetValue(instance);
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                throw new Exception();
            }
        }

        /// <summary>
        ///  (反射) 强制修改目标成员变量
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        /// <param name="memberName"></param>
        /// <exception cref="UnsuccessfullySetValueException"></exception>
        public static void SetValue(object obj, object value, string memberName)
        {
            Type? type;
            object? target = default;
            try
            {
                type = (Type)obj;
            }
            catch (Exception)
            {
                type = obj.GetType();
                target = obj;
            }

            SetValue(type, value, memberName, instance: target);
        }

        /// <summary>
        /// (反射) 强制赋值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="memberName"></param>
        /// <param name="instance"></param>
        /// <exception cref="UnsuccessfullySetValueException"></exception>
        public static void SetValue(Type type, object value, string memberName, object? instance = default)
        {
            MemberInfo[] members = type.GetMember(memberName, ALLKINDS);

            if (members.Length > 0)
            {
                MemberInfo member = members[0];

                try
                {
                    if (member is FieldInfo fieldInfo)
                    {
                        Type targetType = fieldInfo.FieldType;
                        fieldInfo.SetValue(instance, ConvertType(value, targetType));
                    }
                    else if (member is PropertyInfo propertyInfo)
                    {
                        Type targetType = propertyInfo.PropertyType;
                        propertyInfo.SetValue(instance, ConvertType(value, targetType));
                    }
                }
                catch (Exception ex)
                {
                    throw new UnsuccessfullySetValueException(type, memberName, ex);
                }
            }
        }




        /// <summary>
        ///  (反射) 强制类型转换，支持基本类型、数组、枚举、泛型(包括且仅包括List、Queue、Stack、Dictionary、ISet)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static object? ConvertType(object value, Type targetType)
        {
            // 处理泛型
            if (targetType.IsGenericType)
            {
                // 检查是否是IEnumerable<T>的衍生类
                /*var isIE =
                    typeof(IEnumerable<>)
                    .MakeGenericType(targetType.GetGenericArguments())
                    .IsAssignableFrom(targetType);*/

                var GTD = targetType.GetGenericTypeDefinition();

                // List | IEnumerable
                if (GTD == typeof(List<>) || GTD == typeof(IEnumerable<>))
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                    foreach (var item in (IEnumerable)value)
                    {
                        list.Add(ConvertType(item, elementType));
                    }

                    return list;
                }
                // Queue
                else if (GTD == typeof(Queue<>))
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    Queue queue = (Queue)Activator.CreateInstance(typeof(Queue<>).MakeGenericType(elementType));

                    foreach (var item in (IEnumerable)value)
                    {
                        queue.Enqueue(ConvertType(item, elementType));
                    }

                    return queue;
                }
                // Stack
                else if (GTD == typeof(Stack<>))
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    Stack stack = (Stack)Activator.CreateInstance(typeof(Stack<>).MakeGenericType(elementType));

                    foreach (var item in (IEnumerable)value)
                    {
                        stack.Push(ConvertType(item, elementType));
                    }

                    return stack;
                }
                // Dictionary
                else if (GTD == typeof(Dictionary<,>))
                {
                    Type keyType = targetType.GetGenericArguments()[0];
                    Type valueType = targetType.GetGenericArguments()[1];
                    IDictionary dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));

                    foreach (var entry in (IEnumerable)value)
                    {
                        var key = ConvertType(entry.GetType().GetProperty("Key").GetValue(entry), keyType);
                        var val = ConvertType(entry.GetType().GetProperty("Value").GetValue(entry), valueType);
                        dictionary.Add(key, val);
                    }

                    return dictionary;
                }
                // ISet
                else if (targetType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISet<>)))
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    IEnumerable collection = (IEnumerable)value;
                    var set = (ISet<object>)Activator.CreateInstance(targetType);

                    foreach (var item in collection)
                    {
                        set.Add(ConvertType(item, elementType));
                    }

                    return set;
                }
                /*// HashSet
                else if (targetType.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    ICollection hashSet = (ICollection)Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType));

                    foreach (var item in (IEnumerable)value)
                    {
                        hashSet.GetType().GetMethod("Add").Invoke(hashSet, new[] { ConvertType(item, elementType) });
                    }

                    return hashSet;
                }
                // SortedSet
                else if (targetType.GetGenericTypeDefinition() == typeof(SortedSet<>))
                {
                    Type elementType = targetType.GetGenericArguments()[0];
                    IEnumerable collection = (IEnumerable)value;
                    var sortedSet = (ICollection)Activator.CreateInstance(typeof(SortedSet<>).MakeGenericType(elementType));

                    foreach (var item in collection)
                    {
                        sortedSet.GetType().GetMethod("Add").Invoke(sortedSet, new[] { ConvertType(item, elementType) });
                    }

                    return sortedSet;
                }*/
                else
                    return value;
            }
            // 处理数组
            else if (targetType.IsArray)
            {
                Type elementType = targetType.GetElementType();
                var list = (List<object>)ConvertType(value, typeof(List<object>));

                Array array = Array.CreateInstance(elementType, list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    array.SetValue(ConvertType(list[i], elementType), i);
                }

                return array;
            }
            // 处理枚举
            else if (targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }
            else
            {
                try
                {
                    return Convert.ChangeType(value, targetType);
                }
                catch (Exception)
                {
                    return value;
                }
            }
        }

        public static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double);
        }

        /// <summary>
        /// 将object表示的对象还原为Type表示；当其本就不为Type时，调用GetType()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Type ToType(object obj)
        {
            Type? type;
            try
            {
                type = (Type)obj;
            }
            catch (Exception)
            {
                type = obj.GetType();
            }

            return type!;
        }

        /// <summary>
        /// 将JSON对象转换为C#对象
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static object? TransJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Number:
                    // 判断是否为整数
                    return element.TryGetInt64(out long intValue) ? (object)intValue : element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var nestedElement in element.EnumerateObject())
                    {
                        obj.Add(nestedElement.Name, TransJsonElement(nestedElement.Value));
                    }
                    return obj;
                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var nestedElement in element.EnumerateArray())
                    {
                        array.Add(TransJsonElement(nestedElement));
                    }
                    return array;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region SAVE
        /// <summary>
        /// (反射) 使用JSON保存目标的所有[ToSave]成员变量；或者当目标类为[SaveAll]时，保存所有成员变量
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="filePath"></param>
        public static void SaveAllToSaveAsJSON(object obj, string filePath)
        {
            var nameS = GetToSaveMemberName(ToType(obj));

            Dictionary<string, object> dt = [];
            foreach (var name in nameS)
            {
                var value = ReadValue(obj, name);
                if (value != null)
                    dt.Add(name, value);
            }

            SaveToJSON(dt, filePath);
        }


        /// <summary>
        /// 将目标数据序列化为JSON文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        public static void SaveToJSON<T>(T data, string filePath)
        {
            // 将对象序列化为 JSON 字符串
            string jsonData = JsonSerializer.Serialize(data, _Options_JSON_);

            // 将 JSON 字符串保存到文件
            File.WriteAllText(filePath, jsonData);
        }
        #endregion

        #region LOAD
        /// <summary>
        ///(反射) 使用JSON文件的数据修改对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="filePath"></param>
        public static T? LoadFromJSON<T>(T obj, string filePath)
        {
            var orin = LoadFromJSON<Dictionary<string, JsonElement>>(filePath);
            var data = new Dictionary<string, object>();

            if (orin is null)
                return obj;

            foreach (var kvp in orin)
            {
                var value = TransJsonElement(kvp.Value);
                if (value != null)
                    data.Add(kvp.Key, value);
            }

            foreach (var item in data.Keys.ToArray())
            {
                if (data.TryGetValue(item, out var value))
                {
                    try
                    {
                        SetValue(obj, value, item);
                    }
                    catch (Exception e)
                    {
                        string errorText = $"{e.Message}\n因为：{e.InnerException!.Message}\n{e.StackTrace}";
                        Console.WriteLine(errorText);
                        Trace.WriteLine(errorText);
                    }
                }
            };

            return obj;
        }

        /// <summary>
        /// 将JSON文件反序列化为目标对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T? LoadFromJSON<T>(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true,
                };

                // 从文件中读取 JSON 字符串
                string jsonData = File.ReadAllText(filePath);

                // 将 JSON 字符串反序列化为对象
                return JsonSerializer.Deserialize<T>(jsonData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return default;
            }
        }
        #endregion
    }
}
