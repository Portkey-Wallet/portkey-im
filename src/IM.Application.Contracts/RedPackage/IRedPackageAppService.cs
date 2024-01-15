using System;
using System.Threading.Tasks;
using IM.RedPackage.Dtos;
using JetBrains.Annotations;

namespace IM.RedPackage;

public interface IRedPackageAppService
{
    Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput);
    Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto input);
    Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId);
    Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount, int maxResultCount);
    Task<RedPackageConfigOutput> GetRedPackageConfigAsync([CanBeNull] string chainId,
        [CanBeNull] string token);
    Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input);
}