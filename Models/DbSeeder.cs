using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace DoComply.Models
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(DoComplyContext context)
        {
            if (!await context.Database.CanConnectAsync())
            {
                Console.WriteLine("[DbSeeder] Warning: Unable to connect to the database. Please verify the ConnectionString.");
                return;
            }
            Console.WriteLine("[DbSeeder] Connected to database. Verifying seeded records...");

            // 1. Ensure Organization exists
            var org = await context.Organizations.FirstOrDefaultAsync();
            if (org == null)
            {
                org = new Organization
                {
                    OrganizationName = "CC Corporation",
                    SupportEmail = "compliance@cccorp.com",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                context.Organizations.Add(org);
                await context.SaveChangesAsync();
            }

            int orgId = org.OrganizationId;

            // 2. Ensure Password Policy exists
            var policy = await context.PasswordPolicies.FirstOrDefaultAsync(p => p.OrganizationId == orgId);
            if (policy == null)
            {
                policy = new PasswordPolicy
                {
                    OrganizationId = orgId,
                    MinimumLength = 12,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireNumber = true,
                    RequireSpecial = true,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                context.PasswordPolicies.Add(policy);
            }

            // 3. Ensure Notification Settings exist
            var notifSetting = await context.NotificationSettings.FirstOrDefaultAsync(n => n.OrganizationId == orgId);
            if (notifSetting == null)
            {
                notifSetting = new NotificationSetting
                {
                    OrganizationId = orgId,
                    ExpirationRemindersEnabled = true,
                    ExpiredDocumentAlertsEnabled = true,
                    VerificationOutcomesEnabled = true,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                context.NotificationSettings.Add(notifSetting);
            }

            await context.SaveChangesAsync();

            // 4. Ensure System Roles exist (Super Admin, Administrator, Staff / HR, Supplier)
            var existingRoles = await context.Roles.ToListAsync();
            var requiredRoles = new[]
            {
                new { Name = "Super Admin", Desc = "System governance, global settings, and user management" },
                new { Name = "Administrator", Desc = "Compliance officer and document verification administrator" },
                new { Name = "Staff / HR", Desc = "Procurement and HR staff" },
                new { Name = "Supplier", Desc = "Vendor and external contractor portal user" }
            };

            foreach (var req in requiredRoles)
            {
                if (!existingRoles.Any(r => r.RoleName.Equals(req.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Roles.Add(new Role
                    {
                        RoleName = req.Name,
                        Description = req.Desc
                    });
                    await context.SaveChangesAsync();
                }
            }

            existingRoles = await context.Roles.ToListAsync();

            var superAdminRole = existingRoles.FirstOrDefault(r => r.RoleName.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));
            var adminRole = existingRoles.FirstOrDefault(r => r.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) || r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            var staffRole = existingRoles.FirstOrDefault(r => r.RoleName.Equals("Staff / HR", StringComparison.OrdinalIgnoreCase) || r.RoleName.Equals("Staff", StringComparison.OrdinalIgnoreCase));
            var supplierRole = existingRoles.FirstOrDefault(r => r.RoleName.Equals("Supplier", StringComparison.OrdinalIgnoreCase));

            byte superAdminRoleId = superAdminRole?.RoleId ?? 1;
            byte adminRoleId = adminRole?.RoleId ?? 2;
            byte staffRoleId = staffRole?.RoleId ?? 3;
            byte supplierRoleId = supplierRole?.RoleId ?? 4;

            // 5. Update seed passwords with BCrypt hashes
            string superAdminHash = BCrypt.Net.BCrypt.HashPassword("SuperSecure123!");
            string adminHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123!");
            string staffHash = BCrypt.Net.BCrypt.HashPassword("StaffPass123!");
            string supplierHash = BCrypt.Net.BCrypt.HashPassword("SupplierPass123!");

            // Update existing users if password is not a valid bcrypt hash
            var users = await context.Users.ToListAsync();
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.PasswordHash) || !user.PasswordHash.StartsWith("$2"))
                {
                    if (user.RoleId == superAdminRoleId)
                        user.PasswordHash = superAdminHash;
                    else if (user.RoleId == adminRoleId)
                        user.PasswordHash = adminHash;
                    else if (user.RoleId == staffRoleId)
                        user.PasswordHash = staffHash;
                    else
                        user.PasswordHash = supplierHash;

                    user.MustChangePassword = false;
                    user.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            // Ensure prototype-standard accounts exist so quick-click role logins work
            // Super Admin: superadmin@cccorp.com & maria@cccorp.com
            if (!users.Any(u => u.Username.Equals("superadmin@cccorp.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User
                {
                    OrganizationId = orgId,
                    RoleId = superAdminRoleId,
                    FullName = "Elena Rostova",
                    Username = "superadmin@cccorp.com",
                    PasswordHash = superAdminHash,
                    Status = "Active",
                    MustChangePassword = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            // Staff: hr@cccorp.com & leah@cccorp.com
            if (!users.Any(u => u.Username.Equals("hr@cccorp.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User
                {
                    OrganizationId = orgId,
                    RoleId = staffRoleId,
                    FullName = "Leah Ramos",
                    Username = "hr@cccorp.com",
                    PasswordHash = staffHash,
                    Status = "Active",
                    MustChangePassword = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            // Ensure Admin: juan@cccorp.com exists
            if (!users.Any(u => u.Username.Equals("juan@cccorp.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User
                {
                    OrganizationId = orgId,
                    RoleId = adminRoleId,
                    FullName = "Juan Dela Cruz",
                    Username = "juan@cccorp.com",
                    PasswordHash = adminHash,
                    Status = "Active",
                    MustChangePassword = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            // Supplier: abc@company.com
            if (!users.Any(u => u.Username.Equals("abc@company.com", StringComparison.OrdinalIgnoreCase)))
            {
                context.Users.Add(new User
                {
                    OrganizationId = orgId,
                    RoleId = supplierRoleId,
                    FullName = "ABC Office Supplies",
                    Username = "abc@company.com",
                    PasswordHash = supplierHash,
                    Status = "Active",
                    MustChangePassword = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            // Refresh user list for relationships
            var allUsers = await context.Users.ToListAsync();
            var adminUser = allUsers.FirstOrDefault(u => u.RoleId == adminRoleId || u.Username == "juan@cccorp.com") ?? allUsers.First();
            var staffUser = allUsers.FirstOrDefault(u => u.RoleId == staffRoleId) ?? adminUser;
            var supplierUser = allUsers.FirstOrDefault(u => u.RoleId == supplierRoleId) ?? adminUser;

            // 5. Ensure Core Suppliers exist
            var supplierList = new[]
            {
                new { Name = "ABC Office Supplies", Contact = "Juan Dela Cruz", Email = "abc@gmail.com" },
                new { Name = "XYZ Training Services", Contact = "Leah Ramos", Email = "hello@xyztraining.com" },
                new { Name = "DEF Equipment Corp.", Contact = "Mark Anthony", Email = "support@defequip.com" },
                new { Name = "Global Logistics Inc.", Contact = "Sophia Reyes", Email = "info@globallogistics.com" }
            };

            var existingSuppliers = await context.Suppliers.ToListAsync();
            foreach (var s in supplierList)
            {
                if (!existingSuppliers.Any(es => es.SupplierName.Equals(s.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    var newSup = new Supplier
                    {
                        OrganizationId = orgId,
                        SupplierName = s.Name,
                        ContactName = s.Contact,
                        Email = s.Email,
                        Status = "Active",
                        CreatedByUserId = adminUser.UserId,
                        RegisteredAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    context.Suppliers.Add(newSup);
                }
            }
            await context.SaveChangesAsync();

            // 6. Ensure Requirements exist
            var reqNames = new[]
            {
                ("Business Permit", "Supplier", true),
                ("BIR Registration", "Supplier", false),
                ("Tax Clearance", "Supplier", true),
                ("Safety Certificate", "Supplier", true),
                ("Data Privacy Agreement", "Supplier", false)
            };

            var existingReqs = await context.Requirements.ToListAsync();
            foreach (var (rName, appliesTo, expiryReq) in reqNames)
            {
                if (!existingReqs.Any(r => r.RequirementName.Equals(rName, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Requirements.Add(new Requirement
                    {
                        OrganizationId = orgId,
                        RequirementName = rName,
                        AppliesTo = appliesTo,
                        ExpiryRequired = expiryReq,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();

            var currentSuppliers = await context.Suppliers.ToListAsync();
            var currentReqs = await context.Requirements.ToListAsync();
            var dummyHash = new byte[32];

            // 7. Ensure Document Submissions exist for suppliers
            if (!await context.DocumentSubmissions.AnyAsync())
            {
                var abc = currentSuppliers.FirstOrDefault(s => s.SupplierName == "ABC Office Supplies");
                var xyz = currentSuppliers.FirstOrDefault(s => s.SupplierName == "XYZ Training Services");
                var def = currentSuppliers.FirstOrDefault(s => s.SupplierName == "DEF Equipment Corp.");
                var global = currentSuppliers.FirstOrDefault(s => s.SupplierName == "Global Logistics Inc.");

                var reqPermit = currentReqs.FirstOrDefault(r => r.RequirementName == "Business Permit");
                var reqBir = currentReqs.FirstOrDefault(r => r.RequirementName == "BIR Registration");
                var reqTax = currentReqs.FirstOrDefault(r => r.RequirementName == "Tax Clearance");
                var reqSafety = currentReqs.FirstOrDefault(r => r.RequirementName == "Safety Certificate");
                var reqPrivacy = currentReqs.FirstOrDefault(r => r.RequirementName == "Data Privacy Agreement");

                // ABC Office Supplies submissions
                if (abc != null)
                {
                    if (reqPermit != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = abc.SupplierId,
                            RequirementId = reqPermit.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "ABC_BusinessPermit_2026.pdf",
                            StorageObjectKey = "docs/abc_business_permit.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1843200,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2026, 12, 31),
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-30),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-28),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqBir != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = abc.SupplierId,
                            RequirementId = reqBir.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "ABC_BIR_Certificate.pdf",
                            StorageObjectKey = "docs/abc_bir.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 943200,
                            FileSha256 = dummyHash,
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-30),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-28),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqTax != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = abc.SupplierId,
                            RequirementId = reqTax.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "ABC_TaxClearance_2026.pdf",
                            StorageObjectKey = "docs/abc_tax.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1143200,
                            FileSha256 = dummyHash,
                            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)),
                            Status = "Expiring Soon",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-40),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-38),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqSafety != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = abc.SupplierId,
                            RequirementId = reqSafety.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "ABC_SafetyCertificate.pdf",
                            StorageObjectKey = "docs/abc_safety.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1543200,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2027, 1, 31),
                            Status = "Pending Verification",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddHours(-2),
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                // XYZ Training Services submissions
                if (xyz != null && reqTax != null)
                {
                    context.DocumentSubmissions.Add(new DocumentSubmission
                    {
                        SupplierId = xyz.SupplierId,
                        RequirementId = reqTax.RequirementId,
                        VersionNumber = 1,
                        OriginalFileName = "XYZ_TaxClearance_Expired.pdf",
                        StorageObjectKey = "docs/xyz_tax.pdf",
                        StorageProvider = "Cloudinary",
                        StorageResourceType = "raw",
                        ContentType = "application/pdf",
                        FileSizeBytes = 1200000,
                        FileSha256 = dummyHash,
                        ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                        Status = "Expired",
                        SubmittedByUserId = supplierUser.UserId,
                        SubmittedAtUtc = DateTime.UtcNow.AddDays(-60),
                        ReviewedAtUtc = DateTime.UtcNow.AddDays(-55),
                        ReviewedByUserId = adminUser.UserId,
                        UpdatedAtUtc = DateTime.UtcNow
                    });

                    if (reqPermit != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = xyz.SupplierId,
                            RequirementId = reqPermit.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "XYZ_BusinessPermit_2026.pdf",
                            StorageObjectKey = "docs/xyz_permit.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1300000,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2026, 12, 31),
                            Status = "Pending Verification",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddHours(-1),
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                // DEF Equipment Corp submissions
                if (def != null)
                {
                    if (reqTax != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = def.SupplierId,
                            RequirementId = reqTax.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "DEF_TaxClearance.pdf",
                            StorageObjectKey = "docs/def_tax.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 980000,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2026, 12, 31),
                            Status = "Pending Verification",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1),
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqSafety != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = def.SupplierId,
                            RequirementId = reqSafety.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "DEF_SafetyCertificate.pdf",
                            StorageObjectKey = "docs/def_safety.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1100000,
                            FileSha256 = dummyHash,
                            Status = "Rejected",
                            RejectionReason = "Expired certification stamp",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-3),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-2),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                // Global Logistics Inc submissions (All Valid => Compliant)
                if (global != null)
                {
                    if (reqPermit != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = global.SupplierId,
                            RequirementId = reqPermit.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "Global_BusinessPermit_2026.pdf",
                            StorageObjectKey = "docs/global_permit.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 2000000,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2026, 12, 31),
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-20),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-19),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqBir != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = global.SupplierId,
                            RequirementId = reqBir.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "Global_BIR_2026.pdf",
                            StorageObjectKey = "docs/global_bir.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 800000,
                            FileSha256 = dummyHash,
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-20),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-19),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqTax != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = global.SupplierId,
                            RequirementId = reqTax.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "Global_TaxClearance_2027.pdf",
                            StorageObjectKey = "docs/global_tax.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1200000,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2027, 6, 30),
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-20),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-19),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqSafety != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = global.SupplierId,
                            RequirementId = reqSafety.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "Global_SafetyCertificate.pdf",
                            StorageObjectKey = "docs/global_safety.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 1500000,
                            FileSha256 = dummyHash,
                            ExpiryDate = new DateOnly(2027, 3, 31),
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-20),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-19),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }

                    if (reqPrivacy != null)
                    {
                        context.DocumentSubmissions.Add(new DocumentSubmission
                        {
                            SupplierId = global.SupplierId,
                            RequirementId = reqPrivacy.RequirementId,
                            VersionNumber = 1,
                            OriginalFileName = "Global_PrivacyAgreement.pdf",
                            StorageObjectKey = "docs/global_privacy.pdf",
                            StorageProvider = "Cloudinary",
                            StorageResourceType = "raw",
                            ContentType = "application/pdf",
                            FileSizeBytes = 700000,
                            FileSha256 = dummyHash,
                            Status = "Valid",
                            SubmittedByUserId = supplierUser.UserId,
                            SubmittedAtUtc = DateTime.UtcNow.AddDays(-20),
                            ReviewedAtUtc = DateTime.UtcNow.AddDays(-19),
                            ReviewedByUserId = adminUser.UserId,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                await context.SaveChangesAsync();
            }

            // 8. Ensure Procurement Requests exist
            if (!await context.ProcurementRequests.AnyAsync())
            {
                var abc = currentSuppliers.FirstOrDefault(s => s.SupplierName == "ABC Office Supplies");
                var xyz = currentSuppliers.FirstOrDefault(s => s.SupplierName == "XYZ Training Services");
                var def = currentSuppliers.FirstOrDefault(s => s.SupplierName == "DEF Equipment Corp.");
                var global = currentSuppliers.FirstOrDefault(s => s.SupplierName == "Global Logistics Inc.");

                if (abc != null)
                {
                    context.ProcurementRequests.Add(new ProcurementRequest
                    {
                        OrganizationId = orgId,
                        SupplierId = abc.SupplierId,
                        RequestedByUserId = staffUser.UserId,
                        RequestName = "Office Materials Batch A",
                        Purpose = "Office materials for the department including high-yield toner cartridges, recycled printer paper, and stationery bundles for Q4 operations.",
                        Status = "Pending Approval",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                    });
                }

                if (xyz != null)
                {
                    context.ProcurementRequests.Add(new ProcurementRequest
                    {
                        OrganizationId = orgId,
                        SupplierId = xyz.SupplierId,
                        RequestedByUserId = staffUser.UserId,
                        RequestName = "Q4 Safety & Skill Training",
                        Purpose = "Annual safety compliance workshop for staff and hazard mitigation refresher.",
                        Status = "Pending Approval",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                    });
                }

                if (def != null)
                {
                    context.ProcurementRequests.Add(new ProcurementRequest
                    {
                        OrganizationId = orgId,
                        SupplierId = def.SupplierId,
                        RequestedByUserId = staffUser.UserId,
                        RequestName = "Industrial Printers & Scanners",
                        Purpose = "Replacement heavy-duty scanners for logistics hub and warehouse intake desks.",
                        Status = "Pending Approval",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
                    });
                }

                if (global != null)
                {
                    context.ProcurementRequests.Add(new ProcurementRequest
                    {
                        OrganizationId = orgId,
                        SupplierId = global.SupplierId,
                        RequestedByUserId = staffUser.UserId,
                        RequestName = "Freight Shipping Services",
                        Purpose = "Inter-branch freight shipping schedule for November inventory distribution.",
                        Status = "Approved",
                        ApprovedByUserId = adminUser.UserId,
                        ApprovedAtUtc = DateTime.UtcNow.AddDays(-1),
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
                    });
                }

                await context.SaveChangesAsync();
            }

            // 9. Ensure Notifications exist and belong to the correct users/roles
            var superAdminUser = allUsers.FirstOrDefault(u => u.RoleId == 1) ?? adminUser;
            var supplierUsers = allUsers.Where(u => u.RoleId == 4).ToList();

            // Clean up any improperly assigned supplier notifications previously given to adminUser
            var wronglyAssignedToAdmin = await context.Notifications
                .Where(n => n.UserId == adminUser.UserId && (n.Message.StartsWith("Your ") || n.NotificationType == "warning" || n.NotificationType == "danger"))
                .ToListAsync();
            foreach (var wrongNotif in wronglyAssignedToAdmin)
            {
                wrongNotif.UserId = supplierUser.UserId;
            }
            await context.SaveChangesAsync();

            // Ensure Supplier Users have their notifications
            foreach (var supU in supplierUsers)
            {
                if (!await context.Notifications.AnyAsync(n => n.UserId == supU.UserId))
                {
                    var matchingSup = currentSuppliers.FirstOrDefault(s => s.SupplierName == supU.FullName) ?? currentSuppliers.FirstOrDefault();

                    context.Notifications.Add(new Notification
                    {
                        UserId = supU.UserId,
                        SupplierId = matchingSup?.SupplierId,
                        NotificationType = "warning",
                        Message = "Your Tax Clearance will expire soon. Please submit an updated document.",
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
                    });

                    context.Notifications.Add(new Notification
                    {
                        UserId = supU.UserId,
                        SupplierId = matchingSup?.SupplierId,
                        NotificationType = "danger",
                        Message = "Your Safety Certificate has expired. Please upload a renewed document.",
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
                    });

                    context.Notifications.Add(new Notification
                    {
                        UserId = supU.UserId,
                        SupplierId = matchingSup?.SupplierId,
                        NotificationType = "danger",
                        Message = "Your submitted Business Permit was rejected. Please review and submit a valid document.",
                        IsRead = false,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                    });

                    context.Notifications.Add(new Notification
                    {
                        UserId = supU.UserId,
                        SupplierId = matchingSup?.SupplierId,
                        NotificationType = "success",
                        Message = "Your Safety Certificate was verified and approved by Juan Dela Cruz.",
                        IsRead = true,
                        ReadAtUtc = DateTime.UtcNow.AddDays(-1),
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                    });
                }
            }

            // Ensure Admin User has admin notifications
            if (!await context.Notifications.AnyAsync(n => n.UserId == adminUser.UserId && !n.Message.StartsWith("Your ")))
            {
                var abcSup = currentSuppliers.FirstOrDefault(s => s.SupplierName == "ABC Office Supplies");
                var defSup = currentSuppliers.FirstOrDefault(s => s.SupplierName == "DEF Equipment Corp.");

                context.Notifications.Add(new Notification
                {
                    UserId = adminUser.UserId,
                    SupplierId = abcSup?.SupplierId,
                    NotificationType = "info",
                    Message = "ABC Office Supplies submitted Safety Certificate for verification.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
                });

                context.Notifications.Add(new Notification
                {
                    UserId = adminUser.UserId,
                    SupplierId = abcSup?.SupplierId,
                    NotificationType = "info",
                    Message = "Staff (Leah Ramos) submitted a new procurement request for Office Materials Batch A.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-3)
                });

                context.Notifications.Add(new Notification
                {
                    UserId = adminUser.UserId,
                    SupplierId = defSup?.SupplierId,
                    NotificationType = "info",
                    Message = "DEF Equipment Corp submitted Tax Clearance for verification.",
                    IsRead = true,
                    ReadAtUtc = DateTime.UtcNow.AddHours(-12),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                });
            }

            // Ensure Staff User has staff notifications
            if (!await context.Notifications.AnyAsync(n => n.UserId == staffUser.UserId))
            {
                context.Notifications.Add(new Notification
                {
                    UserId = staffUser.UserId,
                    NotificationType = "success",
                    Message = "Procurement request 'Freight Shipping Services' was approved by Administrator.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-4)
                });

                context.Notifications.Add(new Notification
                {
                    UserId = staffUser.UserId,
                    NotificationType = "info",
                    Message = "Procurement request 'Office Materials Batch A' is pending review by Compliance Admin.",
                    IsRead = true,
                    ReadAtUtc = DateTime.UtcNow.AddHours(-18),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                });
            }

            // Ensure SuperAdmin User has superadmin notifications
            if (!await context.Notifications.AnyAsync(n => n.UserId == superAdminUser.UserId))
            {
                context.Notifications.Add(new Notification
                {
                    UserId = superAdminUser.UserId,
                    NotificationType = "info",
                    Message = "Updated system notification settings and organization profile.",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-5)
                });

                context.Notifications.Add(new Notification
                {
                    UserId = superAdminUser.UserId,
                    NotificationType = "info",
                    Message = "Security audit: 4 active user logins recorded today.",
                    IsRead = true,
                    ReadAtUtc = DateTime.UtcNow.AddHours(-10),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                });
            }

            await context.SaveChangesAsync();


            // 10. Ensure Audit Log has initial seed if empty
            if (!await context.AuditLogs.AnyAsync())
            {
                context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = orgId,
                    UserDisplayName = "Administrator",
                    Action = "Approved document (Business Permit)",
                    Module = "Compliance",
                    OccurredAtUtc = DateTime.UtcNow.AddMinutes(-40)
                });
                context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = orgId,
                    UserDisplayName = "Supplier (ABC Office Supplies)",
                    Action = "Uploaded document (Safety Certificate)",
                    Module = "Compliance",
                    OccurredAtUtc = DateTime.UtcNow.AddHours(-1)
                });
                context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = orgId,
                    UserDisplayName = "Administrator",
                    Action = "Added supplier (XYZ Training Services)",
                    Module = "Supplier",
                    OccurredAtUtc = DateTime.UtcNow.AddDays(-1)
                });
                context.AuditLogs.Add(new AuditLog
                {
                    OrganizationId = orgId,
                    UserDisplayName = "HR (Leah Ramos)",
                    Action = "Created procurement request (Q4 Training)",
                    Module = "Procurement",
                    OccurredAtUtc = DateTime.UtcNow.AddDays(-1)
                });
                await context.SaveChangesAsync();
            }

            Console.WriteLine("[DbSeeder] All default organizations, users, suppliers, documents, and audit logs verified.");
        }
    }
}
