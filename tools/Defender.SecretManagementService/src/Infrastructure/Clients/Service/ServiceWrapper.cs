//using AutoMapper;
//using Defender.Common.Clients.Identity;
//using Defender.Common.Wrapper;
//using Defender.SecretManagementService.Application.Common.Interfaces.Wrapper;

//namespace Defender.SecretManagementService.Infrastructure.Clients.Service;

//public class ServiceWrapper : BaseSwaggerWrapper, IServiceWrapper
//{
//    private readonly IMapper _mapper;
//    private readonly IIdentityAsServiceClient _userManagementClient;

//    public ServiceWrapper(
//        IIdentityAsServiceClient userManagementClient,
//        IMapper mapper)
//    {
//        _userManagementClient = userManagementClient;
//        _mapper = mapper;
//    }

//    public Task DoWrap()
//    {
//        //var createCommand = new CreateUserCommand()
//        //{
//        //    Email = user.Email,
//        //    PhoneNumber = user.PhoneNumber,
//        //    Nickname = user.Nickname
//        //};

//        //return await ExecuteSafelyAsync(async () =>
//        //{
//        //    var response = await _userManagementClient.CreateAsync(createCommand);

//        //    return _mapper.Map<Common.DTOs.UserDto>(response);
//        //});
//        throw new NotImplementedException();
//    }
//}
