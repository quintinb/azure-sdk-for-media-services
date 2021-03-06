﻿//-----------------------------------------------------------------------
// <copyright file="TestCloudMediaDataContext.cs" company="Microsoft">Copyright 2012 Microsoft Corporation</copyright>
// <license>
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </license>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.MediaServices.Client.Tests.Helpers
{
    internal class TestCloudMediaDataContext : IMediaDataServiceContext
    {
        private readonly MediaContextBase _mediaContextBase;
        private readonly Dictionary<string, Type> _entitySetMappings = new Dictionary<string, Type>();

        //Tracking all pending changes.
        //Please note that none committed changes are returned by queries
        private readonly Dictionary<string, object> _pendingChanges = new Dictionary<string, object>();
       
        public TestCloudMediaDataContext(MediaContextBase mediaContextBase)
        {
            _mediaContextBase = mediaContextBase;
            _entitySetMappings.Add(JobBaseCollection.JobSet, typeof (JobData));
            _entitySetMappings.Add(AssetCollection.AssetSet, typeof (AssetData));
            _entitySetMappings.Add(AssetFileCollection.FileSet, typeof (AssetFileData));
            _entitySetMappings.Add(AccessPolicyBaseCollection.AccessPolicySet, typeof (AccessPolicyData));
            _entitySetMappings.Add(ContentKeyCollection.ContentKeySet, typeof (ContentKeyData));
            _entitySetMappings.Add(LocatorBaseCollection.LocatorSet, typeof (LocatorData));
        }

        public bool IgnoreResourceNotFoundException { get; set; }

        public IQueryable<T> CreateQuery<T>(string entitySetName)
        {
            if (_pendingChanges.ContainsKey(entitySetName))
            {
                object list = _pendingChanges[entitySetName];

                IEnumerable enumerable = list as IEnumerable;
                return enumerable.OfType<T>().AsQueryable();
            }

            return new List<T>().AsQueryable();
        }

        public void AttachTo(string entitySetName, object entity)
        {
            const string methodName = "AttachTo";
            MethodInfo methodInfo = MakeMethodGeneric(entity, methodName);
            methodInfo.Invoke(this, new[] {entitySetName, entity});
        }

        public void AttachTo(string entitySetName, object entity, string etag)
        {
            throw new NotImplementedException();
        }

        public void DeleteObject(object entity)
        {
            const string methodName = "DeleteObject";
            MethodInfo methodInfo = MakeMethodGeneric(entity, methodName);
            methodInfo.Invoke(this, new[] {entity});
        }

        public IEnumerable<TElement> Execute<TElement>(Uri requestUri)
        {
            string response = "7D9BB04D9D0A4A24800CADBFEF232689E048F69C";
            return new List<TElement>
            {
                (TElement) ((object) response)
            };
        }

        public OperationResponse Execute(Uri requestUri, string httpMethod, params OperationParameter[] operationParameters)
        {
            throw new NotImplementedException();
        }

        public void AddObject(string entitySetName, object entity)
        {
            MethodInfo methodInfo = this.GetType().GetMethods().Where(c => c.Name == "AddObject" && c.IsGenericMethod).First();
            methodInfo = methodInfo.MakeGenericMethod(new[] {entity.GetType()});
            methodInfo.Invoke(this, new[] {entitySetName, entity});
        }

        public QueryOperationResponse LoadProperty(object entity, string propertyName)
        {
            //Example of Load property simulation
            if (entity is AssetData)
            { 
                AssetData assetData = (AssetData)(entity);
                switch (propertyName)
                {
                    case "Locators":
                        assetData.Locators = new List<LocatorData>(CreateQuery<LocatorData>("Locators").Where(c=>c.AssetId ==c.AssetId ).ToList());
                        break;
                    default: break;
                }
            }
            return null;
        }

        public void UpdateObject(object entity)
        {
        }

        public void SetLink(object source, string sourceProperty, object target)
        {
            var locator = source as LocatorData;
            var asset = target as AssetData;
            if (locator!=null && asset!=null)
            {
                asset.Locators.Add(locator);
            }
        }

        public void AddLink(object source, string sourceProperty, object target)
        {
        }

        public IMediaDataServiceResponse SaveChanges()
        {
            return null;
        }

        public void DeleteLink(object source, string sourceProperty, object target)
        {
            throw new NotImplementedException();
        }

        public void AddRelatedObject(object source, string sourceProperty, object target)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> ExecuteAsync<T>(DataServiceQueryContinuation<T> continuation, object state)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> ExecuteAsync<T>(Uri requestUri, object state)
        {
            throw new NotImplementedException();
        }

        public Task<OperationResponse> ExecuteAsync(Uri requestUri, object state, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public Task<DataServiceResponse> ExecuteBatchAsync(object state, params DataServiceRequest[] queries)
        {
            throw new NotImplementedException();
        }

        public Task<DataServiceStreamResponse> GetReadStreamAsync(object entity, DataServiceRequestArgs args, object state)
        {
            throw new NotImplementedException();
        }

        public Task<QueryOperationResponse> LoadPropertyAsync(object entity, string propertyName, object state)
        {
            throw new NotImplementedException();
        }

        public Task<QueryOperationResponse> LoadPropertyAsync(object entity, string propertyName, DataServiceQueryContinuation continuation, object state)
        {
            throw new NotImplementedException();
        }

        public Task<QueryOperationResponse> LoadPropertyAsync(object entity, string propertyName, Uri nextLinkUri, object state)
        {
            throw new NotImplementedException();
        }

        public Task<IMediaDataServiceResponse> SaveChangesAsync(object state)
        {
            return Task.Factory.StartNew((object c) =>
            {
                IMediaDataServiceResponse response = new TestMediaDataServiceResponse();
                response.AsyncState = state;
                state.GetType().InvokeMember("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, state, new[] {"nb:kid:UUID:" + Guid.NewGuid()});
                if (state is IMediaContextContainer)
                {
                    ((IMediaContextContainer) state).SetMediaContext(_mediaContextBase);
                }

                if (state is LocatorData)
                {
                    ((LocatorData) state).Path = "http://contoso.com/" + Guid.NewGuid().ToString();
                }
                if (state is AssetData)
                {
                    ((AssetData)state).Uri = "http://contoso.com/" + Guid.NewGuid().ToString(); 
                }
                return response;
            },
                state,
                CancellationToken.None);
        }

        public Task<IMediaDataServiceResponse> SaveChangesAsync(SaveChangesOptions options, object state)
        {
            throw new NotImplementedException();
        }

        private MethodInfo MakeMethodGeneric(object entity, string methodName)
        {
            MethodInfo methodInfo = this.GetType().GetMethods().Where(c => c.Name == methodName && c.IsGenericMethod).First();
            methodInfo = methodInfo.MakeGenericMethod(new[] {entity.GetType()});
            return methodInfo;
        }

        public void AttachTo<T>(string entitySetName, T entity)
        {
            if (!_pendingChanges.ContainsKey(entitySetName))
            {
                _pendingChanges.Add(entitySetName,
                    new List<T>
                    {
                        entity
                    });
            }
        }

        public void DeleteObject<T>(T entity)
        {
            string entitySetName = this._entitySetMappings.Where(c => c.Value == typeof (T)).FirstOrDefault().Key;

            if (_pendingChanges.ContainsKey(entitySetName))
            {
                ((List<T>) _pendingChanges[entitySetName]).Remove(entity);
            }
        }

        public void AddObject<T>(string entitySetName, T entity)
        {
            if (!_pendingChanges.ContainsKey(entitySetName))
            {
                _pendingChanges.Add(entitySetName,
                    new List<T>
                    {
                        entity
                    });
            }
            else
            {
                List<T> list = _pendingChanges[entitySetName] as List<T>;
                list.Add(entity);
            }
        }
    }
}