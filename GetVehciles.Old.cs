public async Task<List<VehicleBasicInfoDto>> GetVehicles(VehicleFilters filters)
{
    if (filters.IsNull()) new VehicleFilters(_loggedInUser.AlfId);
    else filters.FacilityId = _loggedInUser.AlfId;
    var machineMakes = await this.GetMachineMakes(new MachineMakeFilters(this._loggedInUser.AlfId, MakeType.Vehicle));

    var spec = new VehicleSpec(filters, this._loggedInUser.LoginId, machineMakes);
    var factory = new BaseFactory<Vehicle>(this._loggedInUser, this._dbContext);
    var baseHandler = factory.GeBaseHandlerObject();
    var vehicles = await baseHandler.List(spec);

    if (filters.ProgramId.GetValueOrDefault() > 0 || filters.ProgramIds.IsNotNull() || filters.ManagerId.GetValueOrDefault() > 0 || filters.OnlyAssignedToMe.GetValueOrDefault())
    {
        if (vehicles.IsNotNull())
        {
            var VehicleIds = vehicles.Select(s => s.Id).ToList();
            var assignments = await this.GetVehiclesAssignments(filters, VehicleIds);
            var assignedVehicles = assignments.Select(s => s.MachineId).ToList();
            vehicles = vehicles.Where(a => assignedVehicles.Contains(a.Id)).ToList();
        }
    }

    return vehicles;
}