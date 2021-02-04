using System;
using System.Linq;

namespace ByteFlow.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 用于判断指定类型是否实现指定的泛型接口
        /// </summary>
        /// <param name="type">待判定的类型</param>
        /// <param name="genericType">待验证的泛型接口</param>
        /// <returns>实现了则返回true；否则，返回false</returns>
        public static bool IsInherintGenericTypeInterface(this Type type, Type genericType)
            => type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericType);

        /// <summary>
        /// 用于判断指定类型是否实现指定的泛型接口
        /// </summary>
        /// <param name="type">待判定的类型</param>
        /// <param name="genericType">待验证的泛型接口</param>
        /// <param name="interfaceType">实现的泛型接口的类型，包含泛型参数类型</param>
        /// <returns>实现了则返回true；否则，返回false</returns>
        public static bool IsInherintGenericTypeInterface(this Type type, Type genericType, out Type? interfaceType)
        {
            interfaceType = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericType);
            return interfaceType != null;
        }
    }
}
