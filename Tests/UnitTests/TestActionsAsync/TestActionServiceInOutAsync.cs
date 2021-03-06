﻿// Copyright (c) 2018 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT licence. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using GenericBizRunner;
using GenericBizRunner.Configuration;
using Microsoft.EntityFrameworkCore;
using TestBizLayer.ActionsAsync;
using TestBizLayer.ActionsAsync.Concrete;
using TestBizLayer.BizDTOs;
using TestBizLayer.DbForTransactions;
using Tests.DTOs;
using Tests.Helpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Tests.UnitTests.TestActionsAsync
{
    public class TestActionServiceInOutAsync
    {
        private readonly IGenericBizRunnerConfig _noCachingConfig = new GenericBizRunnerConfig { TurnOffCaching = true };

        //This action does not access the database, but the ActionService checks that the dbContext isn't null
        private readonly DbContext _emptyDbContext = new TestDbContext(SqliteInMemory.CreateOptions<TestDbContext>());

        readonly IMapper _mapper = SetupHelpers.CreateMapper<ServiceLayerBizInDto, ServiceLayerBizOutDto>();


        [Theory]
        [InlineData(123, false)]
        [InlineData(-1, true)]
        public async Task TestActionServiceAsyncInOutDirectOk(int num, bool hasErrors)
        {
            //SETUP 
            var mapper = SetupHelpers.CreateMapper<ServiceLayerBizInDto, ServiceLayerBizOutDto>();
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, mapper, _noCachingConfig);
            var input = new BizDataIn { Num = num };

            //ATTEMPT
            var data = await runner.RunBizActionAsync<BizDataOut>(input);

            //VERIFY
            bizInstance.HasErrors.ShouldEqual(hasErrors);
            if (hasErrors)
            {
                data.ShouldBeNull();
                bizInstance.Message.ShouldEqual("Failed with 1 error");
            }
            else
                data.Output.ShouldEqual(num.ToString());
        }

        [Theory]
        [InlineData(123, false)]
        [InlineData(-1, true)]
        public async Task TestActionServiceAsyncInOutSyncDtosOk(int num, bool hasErrors)
        {
            //SETUP 
            var mapper = SetupHelpers.CreateMapper<ServiceLayerBizInDto, ServiceLayerBizOutDto>();
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, mapper, _noCachingConfig);
            var input = new ServiceLayerBizInDto{Num = num};

            //ATTEMPT
            var data = await runner.RunBizActionAsync<ServiceLayerBizOutDto>(input);

            //VERIFY
            bizInstance.HasErrors.ShouldEqual(hasErrors);
            if (hasErrors)
            {
                data.ShouldBeNull();
                bizInstance.Message.ShouldEqual("Failed with 1 error");
            }
            else
                data.Output.ShouldEqual(num.ToString());
        }

        [Theory]
        [InlineData(123, false)]
        [InlineData(-1, true)]
        public async Task TestActionServiceAsyncInOutAsyncDtosOk(int num, bool hasErrors)
        {
            //SETUP 
            var mapper = SetupHelpers.CreateMapper<ServiceLayerBizInDtoAsync, ServiceLayerBizOutDtoAsync>();
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, mapper, _noCachingConfig);
            var input = new ServiceLayerBizInDtoAsync { Num = num };

            //ATTEMPT
            var data = await runner.RunBizActionAsync<ServiceLayerBizOutDtoAsync>(input);

            //VERIFY
            bizInstance.HasErrors.ShouldEqual(hasErrors);
            if (hasErrors)
            {
                data.ShouldBeNull();
                bizInstance.Message.ShouldEqual("Failed with 1 error");
            }
            else
                data.Output.ShouldEqual(num.ToString());
        }

        [Fact]
        public async Task TestActionServiceErrorInSetupOk()
        {
            //SETUP         
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, _mapper, _noCachingConfig);
            var input = await runner.GetDtoAsync<ServiceLayerBizInDto>(x => { x.RaiseErrorInSetupSecondaryData = true; });

            //ATTEMPT
            await runner.RunBizActionAsync<ServiceLayerBizOutDtoAsync>(input);

            //VERIFY
            bizInstance.HasErrors.ShouldEqual(true);
            bizInstance.Errors.Single().ErrorMessage.ShouldEqual("Error in SetupSecondaryData");
        }

        [Theory]
        [InlineData(123, false)]
        [InlineData(-1, true)]
        public async Task TestActionServiceInOutDatabaseOk(int num, bool hasErrors)
        {
            //SETUP 
            var options = SqliteInMemory.CreateOptions<TestDbContext>();
            using (var context = new TestDbContext(options))
            {
                context.Database.EnsureCreated();

                var bizInstance = new BizActionInOutWriteDbAsync(context);
                var runner = new ActionServiceAsync<IBizActionInOutWriteDbAsync>(context, bizInstance, _mapper, _noCachingConfig);
                var input = new ServiceLayerBizInDto { Num = num };

                //ATTEMPT
                var data = await runner.RunBizActionAsync<BizDataOut>(input);

                //VERIFY
                bizInstance.HasErrors.ShouldEqual(hasErrors);
                if (hasErrors)
                {
                    context.LogEntries.Any().ShouldBeFalse();
                    input.SetupSecondaryDataCalled.ShouldBeFalse();
                    data.ShouldBeNull();
                }
                else
                {
                    context.LogEntries.Single().LogText.ShouldEqual(num.ToString());
                    data.Output.ShouldEqual(num.ToString());
                }
            }
        }

        [Theory]
        [InlineData(123, false)]
        [InlineData(1, true)]
        public async Task TestActionServiceInOutDatabaseValidationOk(int num, bool hasErrors)
        {
            //SETUP 
            var options = SqliteInMemory.CreateOptions<TestDbContext>();
            using (var context = new TestDbContext(options))
            {
                context.Database.EnsureCreated();

                var bizInstance = new BizActionInOutWriteDbAsync(context);
                var runner = new ActionServiceAsync<IBizActionInOutWriteDbAsync>(context, bizInstance, _mapper, _noCachingConfig);
                var input = new ServiceLayerBizInDto { Num = num };

                //ATTEMPT
                var data = await runner.RunBizActionAsync<BizDataOut>(input);

                //VERIFY
                bizInstance.HasErrors.ShouldEqual(hasErrors);
                if (hasErrors)
                {
                    context.LogEntries.Any().ShouldBeFalse();
                    input.SetupSecondaryDataCalled.ShouldBeFalse();
                    data.ShouldBeNull();
                }
                else
                {
                    context.LogEntries.Single().LogText.ShouldEqual(num.ToString());
                    data.Output.ShouldEqual(num.ToString());
                }
            }
        }

        [Fact]
        public async Task TestCallHasNoInputBad()
        {
            //SETUP 
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, _mapper, _noCachingConfig);

            //ATTEMPT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await runner.RunBizActionAsync<string>());

            //VERIFY
            ex.Message.ShouldEqual("Your call of IBizActionInOutAsync needed 'Out, Async' but the Business class had a different setup of 'InOut, Async'");
        }

        [Fact]
        public async Task TestCallHasNoOutputBad()
        {
            //SETUP 
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, _mapper, _noCachingConfig);
            var input = "string";

            //ATTEMPT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await runner.RunBizActionAsync(input));

            //VERIFY
            ex.Message.ShouldEqual("Your call of IBizActionInOutAsync needed 'In, Async' but the Business class had a different setup of 'InOut, Async'");
        }

        //---------------------------------------------------------------
        //error checking

        [Fact]
        public async Task TestInputIsBad()
        {
            //SETUP 
            var bizInstance = new BizActionInOutAsync();
            var runner = new ActionServiceAsync<IBizActionInOutAsync>(_emptyDbContext, bizInstance, _mapper, _noCachingConfig);
            var input = "string";

            //ATTEMPT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await runner.RunBizActionAsync<string>(input));

            //VERIFY
            ex.Message.ShouldEqual("Indirect copy to biz action. from type = String, to type BizDataIn. Expected a DTO of type GenericActionToBizDto<BizDataIn,String>");
        }
    }
}