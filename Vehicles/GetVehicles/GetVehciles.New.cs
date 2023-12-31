public async Task<List<VehicleBasicInfoDto>> GetVehicles(VehicleFilters filters)
{
    filters.FacilityId = _loggedInUser.AlfId;
    List<int?> assignedVehicles = new List<int?>();

    if (ShouldBeFilteredByAssignments(filters))
    {
        assignedVehicles = await FilterVehiclesByAssignments(filters, new List<int>());
        if (assignedVehicles.isValidList()) filters.VehicleIds = assignedVehicles;
    }

    var machineMakes = await GetMachineMakes(filters);
    var vehicles = await GetVehiclesWithSpec(filters, machineMakes);

    return vehicles;
}