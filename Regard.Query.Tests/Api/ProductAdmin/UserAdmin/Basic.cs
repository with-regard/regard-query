using System.Threading.Tasks;
using NUnit.Framework;

namespace Regard.Query.Tests.Api.ProductAdmin.UserAdmin
{
    [TestFixture]
    class Basic
    {
        // The user admin interface doesn't have an API for user status, so there are no assertions yet; we just check that none of this stuff fails
        // Users need to be opted in to run queries, so the query tests will give deeper insights
        // I think we'll want this eventually so we can tell users their current status

        [Test]
        public void CanOptInUser()
        {
            Task.Run(async () =>
            {
                var useradmin = await TestUserAdminFactory.CreateUserAdminForTestProduct();

                await useradmin.OptIn(WellKnownUserIdentifier.TestUser);
            }).Wait();
        }

        [Test]
        public void DoubleOptInIsOk()
        {
            Task.Run(async () =>
            {
                var useradmin = await TestUserAdminFactory.CreateUserAdminForTestProduct();

                await useradmin.OptIn(WellKnownUserIdentifier.TestUser);
                await useradmin.OptIn(WellKnownUserIdentifier.TestUser);
            }).Wait();
        }


        [Test]
        public void CanOptOutUserWhenNotInDatabase()
        {
            Task.Run(async () =>
            {
                var useradmin = await TestUserAdminFactory.CreateUserAdminForTestProduct();

                await useradmin.OptOut(WellKnownUserIdentifier.TestUser);
            }).Wait();
        }

        [Test]
        public void CanOptOutUserAfterOptIn()
        {
            Task.Run(async () =>
            {
                var useradmin = await TestUserAdminFactory.CreateUserAdminForTestProduct();

                await useradmin.OptIn(WellKnownUserIdentifier.TestUser);
                await useradmin.OptOut(WellKnownUserIdentifier.TestUser);
            }).Wait();
        }

        [Test]
        public void DoubleOptOutIsOk()
        {
            Task.Run(async () =>
            {
                var useradmin = await TestUserAdminFactory.CreateUserAdminForTestProduct();

                await useradmin.OptIn(WellKnownUserIdentifier.TestUser);
                await useradmin.OptOut(WellKnownUserIdentifier.TestUser);
                await useradmin.OptOut(WellKnownUserIdentifier.TestUser);
            }).Wait();
        }

    }
}
