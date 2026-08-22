import { Routes } from '@angular/router';
import { Landing } from './pages/landing/landing';
import { Login } from './pages/user/login/login';
import { Register } from './pages/user/register/register';
import { Dashboard } from './pages/dashboard/dashboard';
import { authGuard } from './guards/auth-guard';
import { claimGuard } from './guards/claim-guard';
import { tenantGuard } from './guards/tenant-guard';
import { UserManagement } from './pages/admin/user-management/user-management';
import { ChangePassword } from './pages/change-password/change-password';
import { Profile } from './pages/profile/profile';
import { PersonManagement } from './pages/admin/person-management/person-management';
import { RoleManagement } from './pages/admin/role-management/role-management';
import { TenantPicker } from './pages/tenant-picker/tenant-picker';
import { TenantManagement } from './pages/admin/tenant-management/tenant-management';
import { EmployeeManagement } from './pages/admin/employee-management/employee-management';
import { TenantRoleManagement } from './pages/admin/tenant-role-management/tenant-role-management';
import { EmployeeRoleAssignment } from './pages/admin/employee-role-assignment/employee-role-assignment';
import { MultiTenantSettingsPage } from './pages/admin/multi-tenant-settings/multi-tenant-settings';


export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'tenant-picker', component: TenantPicker, canActivate: [authGuard] },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard, tenantGuard] },
  { path: 'change-password', component: ChangePassword, canActivate: [authGuard] } ,
  { path: 'profile', component: Profile, canActivate: [authGuard, tenantGuard] },

  // Platform (System)
  { path: 'system/platform/tenants', component: TenantManagement, canActivate: [authGuard, claimGuard('TenantsPermission', 'Read')] },
  { path: 'system/platform/multi-tenant-settings', component: MultiTenantSettingsPage, canActivate: [authGuard, claimGuard('MultiTenantSettingsPermission', 'Read')] },

  // Identity (System + Tenant)
  { path: 'system/identity/users', component: UserManagement, canActivate: [authGuard, claimGuard('UserManagementPermission', 'Read')] },
  { path: 'system/identity/roles', component: RoleManagement, canActivate: [authGuard, claimGuard('RolesPermission', 'Read')] },
  { path: 'admin/identity/roles', component: TenantRoleManagement, canActivate: [authGuard, tenantGuard, claimGuard('EmployeesPermission', 'Read')] },
  { path: 'admin/identity/employees/:id/roles', component: EmployeeRoleAssignment, canActivate: [authGuard, tenantGuard, claimGuard('EmployeesPermission', 'Update')] },

  // Organization (Tenant)
  { path: 'admin/organization/employees', component: EmployeeManagement, canActivate: [authGuard, tenantGuard, claimGuard('EmployeesPermission', 'Read')] },
  { path: 'admin/organization/persons', component: PersonManagement, canActivate: [authGuard, tenantGuard, claimGuard('PersonsPermission', 'Read')] },
];
