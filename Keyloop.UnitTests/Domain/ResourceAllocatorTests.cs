using Keyloop.Domain.Entities.Scheduler;
using Keyloop.Domain.Services;

namespace Keyloop.UnitTests.Domain;

public sealed class ResourceAllocatorTests
{
    private static readonly ServiceBay[] Bays =
    [
        new(2, 1, "Bay 2"),
        new(1, 1, "Bay 1")
    ];

    [Fact]
    public void Allocate_SelectsLowestAvailableBayAndQualifiedTechnician()
    {
        var technician = CreateTechnician(2, 1);
        var result = ResourceAllocator.Allocate(
            Bays, [technician], new HashSet<int> { 1 }, new HashSet<int>(), serviceTypeId: 1);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ServiceBay!.Id);
        Assert.Equal(2, result.Technician!.Id);
    }

    [Fact]
    public void Allocate_ReturnsNoBayWhenEveryBayIsBusy()
    {
        var technician = CreateTechnician(1, 1);
        var result = ResourceAllocator.Allocate(
            Bays, [technician], new HashSet<int> { 1, 2 }, new HashSet<int>(), serviceTypeId: 1);

        Assert.False(result.Succeeded);
        Assert.Equal("NO_BAY_AVAILABLE", result.FailureCode);
    }

    [Fact]
    public void Allocate_ReturnsNoTechnicianWhenNobodyIsQualified()
    {
        var technician = CreateTechnician(1, 99);
        var result = ResourceAllocator.Allocate(
            Bays, [technician], new HashSet<int>(), new HashSet<int>(), serviceTypeId: 1);

        Assert.False(result.Succeeded);
        Assert.Equal("NO_TECHNICIAN_AVAILABLE", result.FailureCode);
    }

    [Fact]
    public void Allocate_PrefersTechnicianWithFewerQualifications()
    {
        var specialist = CreateTechnician(1, 1, 2, 3);
        var generalist = CreateTechnician(2, 1);

        var result = ResourceAllocator.Allocate(
            Bays, [specialist, generalist], new HashSet<int>(), new HashSet<int>(), serviceTypeId: 1);

        Assert.Equal(2, result.Technician!.Id);
    }

    private static Technician CreateTechnician(int id, params int[] serviceTypeIds)
    {
        var technician = new Technician(id, 1, $"Technician {id}");
        foreach (var serviceTypeId in serviceTypeIds)
            technician.Qualifications.Add(new TechnicianQualification(id, serviceTypeId));
        return technician;
    }
}
