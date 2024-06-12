namespace MHTools
{
    class StopException : Exception { }
    class LongTimeNoOperationException : Exception { }
    class ResourcesNotFindException : Exception { }

    class UnsuccessfullySetValueException(object obj, string memberName, Exception? ex = default)
        : Exception($"{obj} 对象成员 {memberName} 赋值失败", ex)
    {

    }

}
