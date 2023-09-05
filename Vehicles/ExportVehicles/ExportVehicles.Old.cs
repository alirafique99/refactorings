public class Vehicle
{

    public async Task<List<VehicleExportDto>> Export(VehicleFilters filters)
    {
        if (filters.IsNull()) new VehicleFilters(_loggedInUser.AlfId);
        else filters.FacilityId = _loggedInUser.AlfId;
        var spec = new VehicleExportSpec(filters, this._loggedInUser.LoginId);
        var factory = new BaseFactory<Vehicle>(this._loggedInUser, this._dbContext);
        var baseHandler = factory.GeBaseHandlerObject();

        var vehicles = await baseHandler.List(spec);

        if (vehicles.IsNotNull())
        {
            var VehicleIds = vehicles.Select(s => s.Id).ToList();

            #region Getting Assignment
            var assignments = await this.GetVehiclesAssignments(filters, VehicleIds);
            if (assignments.IsNotNull())
            {
                var assignedVehicles = assignments.Select(s => s.MachineId).ToList();

                if (filters.ProgramId.GetValueOrDefault() > 0 || filters.ProgramIds.IsNotNull() || filters.ManagerId.GetValueOrDefault() > 0 || filters.OnlyAssignedToMe.GetValueOrDefault())
                {
                    vehicles = vehicles.Where(a => assignedVehicles.Contains(a.Id)).ToList();
                }
            }

            foreach (var vehicle in vehicles)
            {
                var assignemet = assignments.Where(a => a.MachineId == vehicle.Id).FirstOrDefault();
                if (assignemet.IsNotNull())
                {
                    vehicle.ManagerId = assignemet.ManagerId;
                    vehicle.ProgramId = assignemet.ProgramId;
                }
            }
            #endregion
            #region Getting Procurements
            var procurements = await this.GetVehiclesProcurements(filters, VehicleIds);

            foreach (var vehicle in vehicles)
            {
                var procurement = procurements.Where(a => a.MachineId == vehicle.Id).FirstOrDefault();
                if (procurement.IsNotNull())
                {
                    vehicle.AcquisitionDate = procurement.AcquisitionDate;
                    vehicle.OwnershipType = procurement.OwnershipType;
                    vehicle.Condition = procurement.Condition;
                    vehicle.MilesOrHours = procurement.MilesOrHours;
                    vehicle.PurchasePrice = procurement.PurchasePrice;
                    vehicle.ProcurementComments = procurement.ProcurementComments;

                    vehicle.LeaseTerm = procurement.Lease.LeaseTerm;
                    vehicle.LeaseTermUnit = procurement.Lease.LeaseTermUnit;
                    vehicle.LeaseMileAllowed = procurement.Lease.LeaseMileAllowed;
                    vehicle.LeaseCompany = procurement.Lease.LeaseCompany;
                    vehicle.LeaseEndDate = procurement.Lease.LeaseEndDate;

                    vehicle.LoanTerm = procurement.Finance.LoanTerm;
                    vehicle.LoanTermUnit = procurement.Finance.LoanTermUnit;
                    vehicle.APR = procurement.Finance.APR;
                    vehicle.FinancingCompany = procurement.Finance.FinancingCompany;
                    vehicle.AccountingCode = procurement.Finance.AccountingCode;
                }
            }
            #endregion
        }

        if (vehicles.IsNotNull())
        {
            var machineMakes = await GetMachineMakes(new MachineMakeFilters(this._loggedInUser.AlfId, MakeType.Vehicle));
            var listOH = await GetOHList();
            var listManager = await GetUsersList();

            foreach (var vehicle in vehicles)
            {
                if (machineMakes.IsNotNull())
                {
                    var makeObj = machineMakes.Find(m => m.Id == Convert.ToInt32(vehicle.Make));
                    if (makeObj.IsNotNull())
                        vehicle.MakeTitle = makeObj.Title;
                }

                if (listOH.IsNotNull())
                {
                    if (vehicle.ProgramId == 0)
                        vehicle.ProgramName = "All";
                    else
                    {
                        var ohObj = listOH.Find(o => o.Id == vehicle.ProgramId);
                        if (ohObj.IsNotNull())
                            vehicle.ProgramName = ohObj.Title;
                    }
                }

                if (listManager.IsNotNull())
                {
                    var managerObj = listManager.Find(m => m.Id == vehicle.ManagerId);
                    if (managerObj.IsNotNull())
                        vehicle.ManagerName = $"{managerObj.FirstName} {managerObj.LastName}";
                }

                var statusObj = await this.GetStatus(new MachineStatusDetailBasic(this._loggedInUser.AlfId, ReferenceTypeId.Fleet_Vehicles, vehicle.Id));
                if (statusObj.IsNotNull())
                {
                    vehicle.Comments = statusObj.Comments;
                    vehicle.Date = statusObj.Date;
                    vehicle.InactiveReason = statusObj.InactiveReason;
                    vehicle.OutOfServiceReason = statusObj.OutOfServiceReason;
                }
            }
        }
        return vehicles;
    }
}