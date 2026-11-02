using System;
using System.Collections.Generic;
using GeminiCode.Thomas.Infra.Data.Mongo.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.Mongo;

public class MongoDbOptionsTests
{
    [Fact]
    public void FromConfiguration_WithConnectionStringAndDatabaseName_ReturnsOptions()
    {
        //arrange
        var configuration = BuildConfiguration(new Dictionary<string, string>
        {
            [MongoDbOptions.ConnectionStringPath] = "mongodb://localhost:27017",
            [MongoDbOptions.DatabaseNamePath] = "ThomasDev"
        });

        //act
        var options = MongoDbOptions.FromConfiguration(configuration);

        //assert
        Assert.Equal("mongodb://localhost:27017", options.ConnectionString);
        Assert.Equal("ThomasDev", options.DatabaseName);
    }

    [Fact]
    public void FromConfiguration_WithoutDatabaseName_UsesTheDatabaseOfTheConnectionString()
    {
        //arrange
        var configuration = BuildConfiguration(new Dictionary<string, string>
        {
            [MongoDbOptions.ConnectionStringPath] = "mongodb://localhost:27017/ThomasDev"
        });

        //act
        var options = MongoDbOptions.FromConfiguration(configuration);

        //assert
        Assert.Equal("ThomasDev", options.DatabaseName);
    }

    [Fact]
    public void FromConfiguration_WithoutConnectionString_ThrowsInvalidOperationException()
    {
        //arrange
        var configuration = BuildConfiguration(new Dictionary<string, string>());

        //act
        var exception = Assert.Throws<InvalidOperationException>(() => MongoDbOptions.FromConfiguration(configuration));

        //assert
        Assert.Contains(MongoDbOptions.ConnectionStringPath, exception.Message);
    }

    [Fact]
    public void FromConfiguration_WithoutDatabaseNameAnywhere_ThrowsInvalidOperationException()
    {
        //arrange
        var configuration = BuildConfiguration(new Dictionary<string, string>
        {
            [MongoDbOptions.ConnectionStringPath] = "mongodb://localhost:27017"
        });

        //act
        var exception = Assert.Throws<InvalidOperationException>(() => MongoDbOptions.FromConfiguration(configuration));

        //assert
        Assert.Contains(MongoDbOptions.DatabaseNamePath, exception.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
