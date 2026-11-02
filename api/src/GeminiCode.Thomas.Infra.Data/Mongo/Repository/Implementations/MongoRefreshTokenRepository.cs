using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Repositories;
using GeminiCode.Thomas.Infra.Data.Mongo.Context;
using GeminiCode.Thomas.Infra.Data.Mongo.Repository.Generic;

namespace GeminiCode.Thomas.Infra.Data.Mongo.Repository.Implementations;

internal class MongoRefreshTokenRepository(MongoDataContext context)
    : MongoBaseEntityRepository<RefreshToken>(context), IRefreshTokenRepository;
