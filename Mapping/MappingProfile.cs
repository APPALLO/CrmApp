using AutoMapper;
using CrmApp.Models;
using CrmApp.Models.DTOs;

namespace CrmApp.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Customer, CustomerDto>().ReverseMap();
        CreateMap<CreateCustomerDto, Customer>();
    }
}
