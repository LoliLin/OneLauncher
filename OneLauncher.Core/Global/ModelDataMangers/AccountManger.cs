﻿using OneLauncher.Core.Global.ModelDataMangers;
using OneLauncher.Core.Helper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OneLauncher.Core.Global.ModelDataMangers;
[JsonSerializable(typeof(AccountData))]
[JsonSerializable(typeof(Dictionary<Guid,UserModel>))]
public partial class AccountDataJsonContext : JsonSerializerContext { }
public class AccountData
{
    public AccountData() {
        UserDictionary = new Dictionary<Guid, UserModel>();
    }
    public Dictionary<Guid, UserModel> UserDictionary { get; set; }
    public Guid? DefaultUserID { get; set; }
}
public class AccountManager : BasicDataManager<AccountData>
{
    public AccountManager(string basePath)
        : base(basePath)
    {
    }

    protected override JsonSerializerContext GetJsonContext() => AccountDataJsonContext.Default;
    protected override Task PostInitialize()
    {
        // 如果没有用户模型，则创建默认
        if (Data.UserDictionary.Count == 0)
        {
            var tmp = Guid.NewGuid();
            Data.UserDictionary[tmp] = new UserModel(tmp, "default", new Guid(UserModel.nullToken));
            Data.DefaultUserID = tmp;
            return Save();
        }
        return Task.CompletedTask;
    }

    public UserModel? GetUser(Guid id) => Data.UserDictionary.GetValueOrDefault(id);

    public IEnumerable<UserModel> GetAllUsers() => Data.UserDictionary.Values;

    public UserModel? GetDefaultUser()
    {
        // 从 this.Data 中获取默认用户ID
        if (Data.DefaultUserID.HasValue)
            return Data.UserDictionary.GetValueOrDefault(Data.DefaultUserID.Value);

        // 如果没有默认用户，可以返回null或者第一个用户作为备选
        return Data.UserDictionary.Values.FirstOrDefault();
    }

    public Task AddUser(UserModel user)
    {
        Data.UserDictionary[user.UserID] = user;
        return Save();
    }

    public Task RemoveUser(Guid userId)
    {
        if (Data.UserDictionary.Count <= 1)
            throw new OlanException("拒绝访问", "你至少要有一个有效的用户模型");

        if (Data.UserDictionary.Remove(userId))
        {
            if (Data.DefaultUserID == userId)
            {
                Data.DefaultUserID = Data.UserDictionary.Keys.FirstOrDefault();
            }
            return Save();
        }
        return Task.CompletedTask;
    }

    public Task SetDefault(Guid userId)
    {
        if (Data.UserDictionary.ContainsKey(userId))
        {
            Data.DefaultUserID = userId;
            return Save();
        }
        return Task.CompletedTask;
    }
}