﻿using System.Threading;
using Microsoft.WindowsAzure.MediaServices.Client.Tests.Helpers;
using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.MediaServices.Client.Tests
{
    [TestClass]
    public class LocatorsTests
    {
        private CloudMediaContext _mediaContext;
        private string _smallWmv;

        /// <summary>
        ///     Gets or sets the test context which provides information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void SetupTest()
        {
            _mediaContext = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            _smallWmv = WindowsAzureMediaServicesTestConfiguration.GetVideoSampleFilePath(TestContext, WindowsAzureMediaServicesTestConfiguration.SmallWmv);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
       
        public void ShouldReturnSameLocatorCollectionAfterAssetRequery()
        {
            var asset = _mediaContext.Assets.Create("interview.wmv", AssetCreationOptions.StorageEncrypted);
            Assert.IsNotNull(asset);
            Assert.AreEqual(0, asset.Locators.Count, "Unexpected locator count");
            var policy = _mediaContext.AccessPolicies.Create("Write", TimeSpan.FromMinutes(5), AccessPermissions.Write);
            var locator = _mediaContext.Locators.CreateSasLocator(asset, policy);

            asset = _mediaContext.Assets.Where(c => c.Id == asset.Id).FirstOrDefault();
            Assert.IsNotNull(asset);
            Assert.AreEqual(1, asset.Locators.Count, "Unexpected locator count");
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
       public void ShouldReturnSameLocatorCollectionAfterAssetRequery2()
        {
            var asset = _mediaContext.Assets.Create(Guid.NewGuid().ToString(), AssetCreationOptions.StorageEncrypted);
            FileInfo info = new FileInfo(@"Content\interview.wmv");
            var file = asset.AssetFiles.Create(info.Name);
            file.Upload(@"Content\interview.wmv");

            Assert.AreEqual(0, asset.Locators.Count, "Unexpected locator count");

            var policy = _mediaContext.AccessPolicies.Create("Write2", TimeSpan.FromMinutes(5), AccessPermissions.Write);
            var locator = _mediaContext.Locators.CreateSasLocator(asset, policy);

            Assert.IsNotNull(asset);
            Assert.AreEqual(1, asset.Locators.Count, "Unexpected locator count");

            asset = _mediaContext.Assets.Where(c => c.Id == asset.Id).First();
            Assert.AreEqual(1, asset.Locators.Count, "Unexpected locator count");
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [ExpectedException(typeof (InvalidOperationException))]
        
        public void ShouldThrowUpdatingLocatorIfItsTypeIsNotOrigin()
        {
            // Arrange

            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromHours(2), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var locator = _mediaContext.Locators.CreateSasLocator(asset, accessPolicy);

            // Act
            locator.Update(DateTime.Now);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldReturnLocatorWhenCreateOriginLocatorCalled()
        {
            // Arrange            


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var actual = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy);

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.IsTrue(actual.ExpirationDateTime < DateTime.UtcNow.AddMinutes(11));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }

        [TestMethod]
        [TestCategory("DailyBvtRun")]
        public void ShouldCreateLocatorWithNameSync()
        {
            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = _mediaContext.Assets.Create("ShouldCreateLocatorAndQueryItByName",AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy,DateTime.Now.AddDays(2),"ShouldCreateLocatorWithoutNameAndUpdateName_" + Guid.NewGuid().ToString());
            
            FindLocatorByNameAndVerifyIt(locator.Name,locator.Id);
        }

        private static void FindLocatorByNameAndVerifyIt(string locatorNameToSearch, string expectedLocatorId)
        {
            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var updated = context2.Locators.Where(c => c.Name == locatorNameToSearch).FirstOrDefault();
            Assert.AreEqual(updated.Id, expectedLocatorId);
        }

        [TestMethod]
        [TestCategory("DailyBvtRun")]
        public void ShouldCreateSASLocatorWithNameSync()
        {
            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = _mediaContext.Assets.Create("ShouldCreateSASLocatorWithName", AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateSasLocator( asset, accessPolicy, DateTime.Now.AddDays(2), "ShouldCreateLocatorWithoutNameAndUpdateName_" + Guid.NewGuid().ToString());

            FindLocatorByNameAndVerifyIt(locator.Name,locator.Id);
        }

        [TestMethod]
        [TestCategory("DailyBvtRun")]
        public void ShouldCreateLocatorWithNameASync()
        {
            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = _mediaContext.Assets.Create("ShouldCreateLocatorAndQueryItByName", AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateLocatorAsync(LocatorType.OnDemandOrigin, asset, accessPolicy, DateTime.Now.AddDays(2), "ShouldCreateLocatorWithoutNameAndUpdateName_" + Guid.NewGuid().ToString()).Result;

            FindLocatorByNameAndVerifyIt(locator.Name,locator.Id);
        }

        [TestMethod]
        [TestCategory("DailyBvtRun")]
        public void ShouldCreateSASLocatorWithNameASync()
        {
            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = _mediaContext.Assets.Create("ShouldCreateSASLocatorWithName", AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateSasLocatorAsync(asset, accessPolicy, DateTime.Now.AddDays(2), "ShouldCreateLocatorWithoutNameAndUpdateName_" + Guid.NewGuid().ToString()).Result;

            FindLocatorByNameAndVerifyIt(locator.Name, locator.Id);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        public void ShouldCreateLocatorWhenCreateOriginLocatorCalled()
        {
            // Arrange            


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy);

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.AreEqual(locator.Id, actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.IsTrue(actual.ExpirationDateTime < DateTime.UtcNow.AddMinutes(11));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldCreateLocatorWhenCreateOriginLocatorAsyncCalled()
        {
            // Arrange         


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var task = _mediaContext.Locators.CreateLocatorAsync(LocatorType.OnDemandOrigin, asset, accessPolicy);
            task.Wait();

            var locator = task.Result;

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.AreEqual(locator.Id, actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.IsTrue(actual.ExpirationDateTime < DateTime.UtcNow.AddMinutes(11));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldReturnLocatorWhenCreateOriginLocatorWithStartDateCalled()
        {
            // Arrange            


            var accessPolicyDuration = TimeSpan.FromMinutes(10);
            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", accessPolicyDuration, AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var startDateTime = DateTime.UtcNow.AddDays(1);
            var expirationDateTime = startDateTime.Add(accessPolicyDuration);

            // Act
            var actual = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy, startDateTime);

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.AreEqual(startDateTime, actual.StartTime);
            Assert.IsTrue(actual.ExpirationDateTime < expirationDateTime.AddMinutes(1));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }


        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldReturnLocatorWhenCreateSasLocatorCalled()
        {
            // Arrange            

            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var actual = _mediaContext.Locators.CreateSasLocator(asset, accessPolicy);

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.IsTrue(actual.ExpirationDateTime < DateTime.UtcNow.AddMinutes(11));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        public void ShouldCreateLocatorWhenCreateSasLocatorCalled()
        {
            // Arrange            

            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var locator = _mediaContext.Locators.CreateSasLocator(asset, accessPolicy);

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.AreEqual(locator.Id, actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.IsTrue(actual.ExpirationDateTime < DateTime.UtcNow.AddMinutes(11));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }


        [TestMethod]
        [TestCategory("DailyBvtRun")]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        public void ExpirationTimeOfCreatedLocatorShouldMatchLocatorStartTimePlusPolicyDuration()
        {
            // Arrange            


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            DateTime locatorStartTime = DateTime.Now.AddHours(1);

            // Act
            var locatorTask = _mediaContext.Locators.CreateLocatorAsync(LocatorType.Sas, asset, accessPolicy, locatorStartTime.ToUniversalTime());
            locatorTask.Wait();
            var locator = locatorTask.Result;

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.AreEqual(locator.Id, actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
            var expectedExpiration = locatorStartTime.Add(accessPolicy.Duration).ToUniversalTime();
            Assert.AreEqual(expectedExpiration, locator.ExpirationDateTime);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        public void ShouldCreateLocatorWhenCreateSasLocatorAsyncCalled()
        {
            // Arrange            


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);

            // Act
            var task = _mediaContext.Locators.CreateSasLocatorAsync(asset, accessPolicy);
            task.Wait();

            var locator = task.Result;

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNotNull(actual);
            Assert.AreEqual(locator.Id, actual.Id);
            Assert.IsNotNull(actual.Path);
            Assert.IsTrue(actual.ExpirationDateTime < DateTime.UtcNow.AddMinutes(11));
            Assert.AreEqual(asset.Id, actual.Asset.Id);
            Assert.AreEqual(asset.Name, actual.Asset.Name);
            Assert.AreEqual(accessPolicy.Id, actual.AccessPolicy.Id);
        }


        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldDeleteLocatorWhenDeleteLocatorCalled()
        {
            // Arrange            

            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateSasLocator(asset, accessPolicy);

            // Act
            locator.Delete();

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNull(actual);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldDeleteLocatorWhenDeleteLocatorAsyncCalled()
        {
            // Arrange            


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", TimeSpan.FromMinutes(10), AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateSasLocator(asset, accessPolicy);

            // Act
            var task = locator.DeleteAsync();
            task.Wait();

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.IsNull(actual);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        public void ShouldUpdateExpiryTimeWhenUpdateLocatorCalledWithExpiryTime()
        {
            // Arrange            
            var accessPolicyDuration = TimeSpan.FromHours(2);
            var expectedExpiryTime = DateTime.UtcNow.Date.AddDays(2);

            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", accessPolicyDuration, AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy);

            // Act
            locator.Update(expectedExpiryTime);

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.AreEqual(expectedExpiryTime, locator.ExpirationDateTime);
            Assert.AreEqual(expectedExpiryTime, actual.ExpirationDateTime);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldUpdateExpiryTimeWhenUpdateLocatorAsyncCalledWithExpiryTime()
        {
            // Arrange            
            
            var accessPolicyDuration = TimeSpan.FromHours(2);
            var expectedExpiryTime = DateTime.UtcNow.Date.AddDays(1);


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", accessPolicyDuration, AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy);

            // Act
            var task = locator.UpdateAsync(expectedExpiryTime);
            task.Wait();

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.AreEqual(expectedExpiryTime, locator.ExpirationDateTime);
            Assert.AreEqual(expectedExpiryTime, actual.ExpirationDateTime);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        
        public void ShouldUpdateExpiryTimeWhenUpdateLocatorCalledWithStartAndExpiryTime()
        {
            // Arrange            
            var accessPolicyDuration = TimeSpan.FromHours(2);
            var expectedExpiryTime = DateTime.UtcNow.Date.AddDays(2);
            var expectedStartTime = DateTime.UtcNow.Date.AddDays(1);


            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", accessPolicyDuration, AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy);

            // Act
            locator.Update(expectedStartTime, expectedExpiryTime);

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.AreEqual(expectedStartTime, locator.StartTime);
            Assert.AreEqual(expectedExpiryTime, locator.ExpirationDateTime);
#if false // Known service bug does not support returning start date
            Assert.AreEqual(expectedStartTime, actual.StartTime);
#endif
            Assert.AreEqual(expectedExpiryTime, actual.ExpirationDateTime);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        
        public void ShouldUpdateExpiryTimeWhenUpdateLocatorAsyncCalledWithStartAndExpiryTime()
        {
            // Arrange          
           
            var accessPolicyDuration = TimeSpan.FromHours(2);
            var expectedExpiryTime = DateTime.UtcNow.Date.AddDays(2);
            var expectedStartTime = DateTime.UtcNow.Date.AddDays(1);

            var accessPolicy = _mediaContext.AccessPolicies.Create("TestPolicy", accessPolicyDuration, AccessPermissions.List | AccessPermissions.Read);
            var asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.None);
            var locator = _mediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, accessPolicy);

            // Act
            var task = locator.UpdateAsync(expectedStartTime, expectedExpiryTime);
            task.Wait();

            var context2 = WindowsAzureMediaServicesTestConfiguration.CreateCloudMediaContext();
            var actual = context2.Locators.Where(x => x.Id == locator.Id).FirstOrDefault();

            // Assert                        
            Assert.AreEqual(expectedStartTime, locator.StartTime);
            Assert.AreEqual(expectedExpiryTime, locator.ExpirationDateTime);
            Assert.AreEqual(expectedExpiryTime, actual.ExpirationDateTime);
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldDeleteLocatorAfterSyncUpload()
        {
            var asset = _mediaContext.Assets.Create("interview.wmv", AssetCreationOptions.None);
            var file = asset.AssetFiles.Create("interview.wmv");
            file.Upload(@"Content\interview.wmv");
            Assert.AreEqual(0, asset.Locators.Count, "No locators expected in asset locators collection after Upload");
            Assert.AreEqual(0, _mediaContext.Locators.Where(c => c.AssetId == asset.Id).Count(), "No locators expected in context queryable after Upload");
        }

        [TestMethod]
        [DeploymentItem(@".\Resources\interview.wmv", "Content")]
        [TestCategory("DailyBvtRun")]
        public void ShouldNotDeleteLocatorAfterASyncUpload()
        {
            var asset = _mediaContext.Assets.Create("interview.wmv", AssetCreationOptions.None);
            var file = asset.AssetFiles.Create("interview.wmv");
            var policy = _mediaContext.AccessPolicies.Create("Write", TimeSpan.FromMinutes(5), AccessPermissions.Write);
            var locator = _mediaContext.Locators.CreateSasLocator(asset, policy);

            file.UploadAsync(@"Content\interview.wmv", new BlobTransferClient(), locator, CancellationToken.None).Wait();
            Assert.AreEqual(1, asset.Locators.Count, "1 locator expected in asset locators collection after Upload");
            Assert.AreEqual(1, _mediaContext.Locators.Where(c => c.AssetId == asset.Id).Count(), "1 locator expected in context queryable after Upload");
        }

        [TestMethod]
        [Priority(0)]
        public void TestLocatorCreateRetry()
        {
            var dataContextMock = new Mock<IMediaDataServiceContext>();

            int exceptionCount = 2;

            var expected = new LocatorData { Name = "testData" };
            var fakeResponse = new TestMediaDataServiceResponse { AsyncState = expected };
            var fakeException = new WebException("testException", WebExceptionStatus.ConnectionClosed);

            dataContextMock.Setup((ctxt) => ctxt.AddObject("Files", It.IsAny<object>()));
            dataContextMock.Setup((ctxt) => ctxt
                .SaveChangesAsync(It.IsAny<object>()))
                .Returns(() => Task.Factory.StartNew<IMediaDataServiceResponse>(() =>
                {
                    if (--exceptionCount > 0) throw fakeException;
                    return fakeResponse;
                }));

            _mediaContext.MediaServicesClassFactory = new TestMediaServicesClassFactory(dataContextMock.Object);

            var asset = new AssetData { Name = "testAssetData" };

            asset.SetMediaContext(_mediaContext);
            ILocator locator = _mediaContext.Locators.CreateLocator(LocatorType.None, asset, new AccessPolicyData());
            Assert.AreEqual(expected.Name, locator.Name);
            Assert.AreEqual(0, exceptionCount);
        }

        [TestMethod]
        [Priority(0)]
        public void TestLocatorUpdateRetry()
        {
            var dataContextMock = new Mock<IMediaDataServiceContext>();

            int exceptionCount = 2;

            var locator = new LocatorData { Name = "testData", Type = (int)LocatorType.OnDemandOrigin };
            var fakeResponse = new TestMediaDataServiceResponse { AsyncState = locator };
            var fakeException = new WebException("test", WebExceptionStatus.ConnectionClosed);

            dataContextMock.Setup((ctxt) => ctxt.AttachTo("Locators", locator));
            dataContextMock.Setup((ctxt) => ctxt.UpdateObject(locator));

            dataContextMock.Setup((ctxt) => ctxt
                .SaveChangesAsync(locator))
                .Returns(() => Task.Factory.StartNew<IMediaDataServiceResponse>(() =>
                {
                    if (--exceptionCount > 0) throw fakeException;
                    return fakeResponse;
                }));

            _mediaContext.MediaServicesClassFactory = new TestMediaServicesClassFactory(dataContextMock.Object);

            locator.SetMediaContext(_mediaContext);

            locator.Update(DateTime.Now);

            Assert.AreEqual(0, exceptionCount);
        }

        [TestMethod]
        [Priority(0)]
        public void TestLocatorDeleteRetry()
        {
            var dataContextMock = new Mock<IMediaDataServiceContext>();

            int exceptionCount = 2;

            var locator = new LocatorData { Name = "testData", Type = (int)LocatorType.OnDemandOrigin };
            var fakeResponse = new TestMediaDataServiceResponse { AsyncState = locator };
            var fakeException = new WebException("test", WebExceptionStatus.ConnectionClosed);

            dataContextMock.Setup((ctxt) => ctxt.AttachTo("Locators", locator));
            dataContextMock.Setup((ctxt) => ctxt.DeleteObject(locator));

            dataContextMock.Setup((ctxt) => ctxt
                .SaveChangesAsync(locator))
                .Returns(() => Task.Factory.StartNew<IMediaDataServiceResponse>(() =>
                {
                    if (--exceptionCount > 0) throw fakeException;
                    return fakeResponse;
                }));

            // Cannot mock DataServiceQuery. Throw artificial exception to mark pass through saving changes.
            string artificialExceptionMessage = "artificialException";
            dataContextMock.Setup((ctxt) => ctxt.CreateQuery<AssetData>("Assets")).Throws(new Exception(artificialExceptionMessage));

            _mediaContext.MediaServicesClassFactory = new TestMediaServicesClassFactory(dataContextMock.Object);

            locator.SetMediaContext(_mediaContext);

            try
            {
            locator.Delete();
            }
            catch (Exception x)
            {
                Assert.AreEqual(artificialExceptionMessage, x.Message);
            }

            Assert.AreEqual(0, exceptionCount);
        }
    }
}
