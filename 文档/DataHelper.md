# DataHelper
## SaveAllAttribute
- 标记一个类，其中所有变量均为存储目标「MHTools.DataHelper」
## SaveAllWithoutAttribute
- 标记一个类，忽略特定类型的变量，剩余所有变量均为存储目标「MHTools.DataHelper」
## ToSaveAttribute
- 标记一个成员变量为数据存储目标「MHTools.DataHelper」
## DoNotSaveAttribute
- 标记一个成员变量为非存储目标。(仅当SaveAll时生效)「MHTools.DataHelper」
## GetToSaveMemberName(System.Type)
- 获取目标类型的所有被标记为"数据存储对象"的成员名称
## ReadValue(object, string)
- (反射)强制获取目标成员变量
## ReadValue(System.Type, string, object?)
- (反射)强制读值
## SetValue(object, object, string)
- (反射)强制修改目标成员变量
## SetValue(System.Type, object, string, object?)
- (反射)强制赋值
## ConvertType(object, System.Type)
- (反射)强制类型转换，支持基本类型、数组、枚举、泛型(包括且仅包括List、Queue、Stack、Dictionary、ISet)
## IsNumericType(System.Type)
## ToType(object)
- 将object表示的对象还原为Type表示；当其本就不为Type时，调用GetType()
## TransJsonElement(System.Text.Json.JsonElement)
- 将JSON对象转换为C#对象
## SaveAllToSaveAsJSON(object, string)
- (反射)使用JSON保存目标的所有[ToSave]成员变量；或者当目标类为[SaveAll]时，保存所有成员变量
## SaveToJSON<T>(T, string)
- 将目标数据序列化为JSON文件
## LoadFromJSON<T>(T, string)
- (反射)使用JSON文件的数据修改对象
## LoadFromJSON<T>(string)
- 将JSON文件反序列化为目标对象
