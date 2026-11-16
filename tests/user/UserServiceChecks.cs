using System;
using System.Collections.Generic;
using System.Reflection;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Services;

public class RepositoryStub : DispatchProxy
{
    public User Result;
    public bool Fail;
    protected override object Invoke(MethodInfo method, object[] args)
    {
        if (Fail) throw new InvalidOperationException("Database unavailable");
        return Result;
    }
}
class Program
{
    static void Main()
    {
        var repository = DispatchProxy.Create<IUserRepository, RepositoryStub>();
        var stub = (RepositoryStub)(object)repository;
        var service = new UserService(repository);
        stub.Result = new User { UserId = "test", ActiveFlag = true, BusinessEntities = null };
        if (service.GetUser("test")?.IsActive != true) throw new Exception("Missing memberships must not prevent loading");
        stub.Result.BusinessEntities = new List<UserEntity> { new UserEntity { EntityId = 2, ConfigAdminFlag = true } };
        if (service.GetUser("test").IsAdmin) throw new Exception("Other entity admin flag leaked");
        stub.Result.BusinessEntities.Add(new UserEntity { EntityId = 1, ConfigAdminFlag = true });
        if (!service.GetUser("test").IsAdmin) throw new Exception("Current entity admin flag missing");
        stub.Result = null;
        if (service.GetUser("missing") != null) throw new Exception("Missing user should return null");
        stub.Fail = true;
        if (service.GetUser(" ") != null) throw new Exception("Blank id should skip lookup");
        try { service.GetUser("test"); throw new Exception("Lookup failure was swallowed"); }
        catch (InvalidOperationException) { }
        Console.WriteLine("PASS: missing memberships, entity admin flags, missing users, blank IDs, lookup failures");
    }
}
namespace FastQ.Web.Services
{
    public class AuthService
    {
        public string GetLoggedInWindowsUser() => "admin";
        public long GetSessionEntityId() => 1;
    }
}
namespace FastQ.Data.Db
{
    public static class DbRepositoryFactory
    {
        public static IUserRepository CreateUserRepository() => throw new NotSupportedException();
    }
}
