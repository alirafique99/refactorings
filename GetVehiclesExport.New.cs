public async Task<List<VehicleExportDto>> GetVehiclesExport(VehicleFilters filters)
{
    filters.FacilityId = _loggedInUser.AlfId;
    
    var assignments = await this.GetVehiclesAssignments(filters, new List<int>());
    if (ShouldFilterByAssignments(filters))
        filters.VehicleIds = assignments.Select(x => x.MachineId).ToList();

    var vehicles = await GetVehiclesExportBySpec(filters);

    if (vehicles.IsNotNull())
    {
        vehicles = SetVehiclesAssignment(vehicles, assignments, filters);
        vehicles = await GetVehiclesProcurements(vehicles, filters);
    }

    if (vehicles.IsNotNull())
    {
        var machineMakes = await GetMachineMakes(filters);
        var listOH = await GetOHList();
        var listManager = await GetUsersList();

        foreach (var vehicle in vehicles)
        {
            SetMachineMakeExport(machineMakes, vehicle);
            SetVehicleAssignmentTitlesExport(listOH, listManager, vehicle);
            await SetVehiclesStatusExport(vehicle);
        }
    }
    return vehicles;
}