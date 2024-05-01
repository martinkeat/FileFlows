using System.Data.Common;
using System.Reflection;
using FileFlows.Shared.Attributes;
using NPoco;

namespace FileFlows.DataLayer.Converters;

/// <summary>
/// Converter that will automatically encrypt and decrypt data
/// </summary>
public class EncryptedConverter:DefaultMapper
{
    /// <summary>
    /// Get the convert for converting an object to the object actually stored in the database
    /// </summary>
    /// <param name="destType">the type we are trying to convert to</param>
    /// <param name="sourceMemberInfo">the information about the type being converted</param>
    /// <returns>the converter function to use</returns>
    public override Func<object, object> GetToDbConverter(Type destType, MemberInfo sourceMemberInfo)
    {
        if (destType != typeof(string))
            return base.GetToDbConverter(destType, sourceMemberInfo);
        var encrypted = sourceMemberInfo.GetCustomAttribute<EncryptedAttribute>();
        if(encrypted == null)
            return base.GetToDbConverter(destType, sourceMemberInfo);
        return x =>
        {
            var value = x.ToString() ?? string.Empty;
            return Helpers.Decrypter.Encrypt(value);
        };
    }

    public override Func<object, object> GetFromDbConverter(MemberInfo destMemberInfo, Type sourceType)
    {
        if (sourceType != typeof(string))
            return base.GetFromDbConverter(destMemberInfo, sourceType);
        var encrypted = destMemberInfo.GetCustomAttribute<EncryptedAttribute>();
        if(encrypted == null)
            return base.GetFromDbConverter(destMemberInfo, sourceType);
        return x =>
        {
            var value = x.ToString();
            return Helpers.Decrypter.Decrypt(value ?? string.Empty);
        };
    }
}