using System;
using System.Collections.Generic;

namespace DoComply
{
    public class UserDataItem
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
    }

    public class SupplierDataItem
    {
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string ComplianceStatus { get; set; } = "COMPLIANT";
        public string ComplianceReasons { get; set; } = "All required documents valid";
    }

    public class RequirementItem
    {
        public string Name { get; set; } = string.Empty;
        public string AppliesTo { get; set; } = "Supplier";
        public bool ExpiryRequired { get; set; }
    }

    public class VerificationQueueItem
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string SubmittedTime { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending Verification";
    }

    public class ExpiryMonitoringItem
    {
        public string SupplierName { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string TimeRemaining { get; set; } = string.Empty;
        public string Status { get; set; } = "Expiring Soon";
    }

    public class ProcurementRequestItem
    {
        public int Id { get; set; }
        public string RequestName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string DateSubmitted { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending Approval";
        public bool IsSupplierCompliant { get; set; } = true;
    }

    public class AuditLogItem
    {
        public string User { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    public static class SampleDataFixture
    {
        public static List<UserDataItem> GetSampleUsers()
        {
            var list = new List<UserDataItem>
            {
                new UserDataItem { Name = "Juan Dela Cruz", Role = "Administrator", Username = "juan@cccorp.com", Status = "Active" },
                new UserDataItem { Name = "ABC Office Supplies", Role = "Supplier", Username = "abc@company.com", Status = "Active" },
                new UserDataItem { Name = "Leah Ramos", Role = "Staff-HR", Username = "leah@cccorp.com", Status = "Active" },
                new UserDataItem { Name = "Elena Rostova", Role = "Super Admin", Username = "elena@cccorp.com", Status = "Active" },
                new UserDataItem { Name = "XYZ Training Services", Role = "Supplier", Username = "xyz@company.com", Status = "Active" },
                new UserDataItem { Name = "DEF Equipment Corp", Role = "Supplier", Username = "def@company.com", Status = "Active" }
            };

            string[] roles = { "Administrator", "Staff-HR", "Supplier", "Super Admin" };
            string[] names = {
                "Carlos Mendoza", "Maria Santos", "David Chen", "Sarah Jenkins", "Michael Chang",
                "Patricia Alvarez", "Robert Taylor", "Amanda Lewis", "James Wilson", "Jennifer Martinez",
                "Apex Industrial Systems", "Beacon Freight Logistics", "Cascade Security Solutions",
                "Delta Tech Services", "EcoClean Solutions", "Falcon Express Cargo", "Global Food Distribution",
                "Horizon Facility Management", "Infinite Cloud Systems", "Junction Media Services",
                "Kinetic Power Grid", "Lighthouse Tech", "Matrix Cyber Analytics", "Nexus Paper Co.",
                "Omni Digital Prints", "Pinnacle Catering", "Quantum Safety Equipment", "Redwood Construction",
                "Summit Maintenance", "Trident Utilities", "United Office Hardware", "Vanguard Protective Services",
                "Wave Interactive", "Xenon Telecoms", "Yorkshire Stationers", "Zenith Chemical Labs",
                "Alpha Uniforms", "Bravo Catering", "Charlie Transport", "Echo Printing"
            };

            int index = 1;
            foreach (var n in names)
            {
                string r = roles[index % roles.Length];
                string cleanName = n.ToLower().Replace(" ", "");
                list.Add(new UserDataItem
                {
                    Name = n,
                    Role = r,
                    Username = $"{cleanName}@org{index}.com",
                    Status = (index % 7 == 0) ? "Inactive" : "Active"
                });
                index++;
            }

            return list;
        }

        public static List<SupplierDataItem> GetSampleSuppliers()
        {
            var list = new List<SupplierDataItem>
            {
                new SupplierDataItem { Name = "ABC Office Supplies", ContactPerson = "Juan Dela Cruz", Email = "abc@gmail.com", Status = "Active", ComplianceStatus = "NOT COMPLIANT", ComplianceReasons = "1 missing · 1 pending verification" },
                new SupplierDataItem { Name = "XYZ Training Services", ContactPerson = "Leah Ramos", Email = "hello@xyztraining.com", Status = "Active", ComplianceStatus = "NOT COMPLIANT", ComplianceReasons = "1 expired document" },
                new SupplierDataItem { Name = "DEF Equipment Corp.", ContactPerson = "Mark Anthony", Email = "support@defequip.com", Status = "Active", ComplianceStatus = "NOT COMPLIANT", ComplianceReasons = "1 rejected document" },
                new SupplierDataItem { Name = "Global Logistics Inc.", ContactPerson = "Sophia Reyes", Email = "info@globallogistics.com", Status = "Active", ComplianceStatus = "COMPLIANT", ComplianceReasons = "All 5 documents verified & valid" }
            };

            string[] supplierCompanyNames = {
                "Apex Industrial Systems", "Beacon Freight Logistics", "Cascade Security Solutions",
                "Delta Tech Services", "EcoClean Solutions", "Falcon Express Cargo", "Global Food Distribution",
                "Horizon Facility Management", "Infinite Cloud Systems", "Junction Media Services",
                "Kinetic Power Grid", "Lighthouse Tech", "Matrix Cyber Analytics", "Nexus Paper Co.",
                "Omni Digital Prints", "Pinnacle Catering", "Quantum Safety Equipment", "Redwood Construction",
                "Summit Maintenance", "Trident Utilities", "United Office Hardware", "Vanguard Protective Services",
                "Wave Interactive", "Xenon Telecoms", "Yorkshire Stationers", "Zenith Chemical Labs",
                "Alpha Uniforms", "Bravo Catering", "Charlie Transport", "Echo Printing",
                "Foxtrot Cleaning", "Golf Electricals", "Hotel Fire Safety", "India Telecom",
                "Juliet Logistics", "Kilo Hardware", "Lima Security", "Mike Catering", "November Supplies"
            };

            string[] contacts = { "Alice Guo", "Benjamin Tan", "Clara Garcia", "Daniel Padilla", "Esther Villa", "Fernando Poe" };

            int idx = 1;
            foreach (var sName in supplierCompanyNames)
            {
                bool isCompliant = (idx % 3 != 0);
                list.Add(new SupplierDataItem
                {
                    Name = sName,
                    ContactPerson = contacts[idx % contacts.Length],
                    Email = $"contact@{sName.ToLower().Replace(" ", "")}.com",
                    Status = "Active",
                    ComplianceStatus = isCompliant ? "COMPLIANT" : "NOT COMPLIANT",
                    ComplianceReasons = isCompliant ? "All 5 documents verified & valid" : (idx % 2 == 0 ? "1 missing document" : "1 pending verification")
                });
                idx++;
            }

            return list;
        }

        public static List<RequirementItem> GetSampleRequirements()
        {
            return new List<RequirementItem>
            {
                new RequirementItem { Name = "Business Permit", AppliesTo = "Supplier", ExpiryRequired = true },
                new RequirementItem { Name = "BIR Registration", AppliesTo = "Supplier", ExpiryRequired = false },
                new RequirementItem { Name = "Tax Clearance", AppliesTo = "Supplier", ExpiryRequired = true },
                new RequirementItem { Name = "Safety Certificate", AppliesTo = "Supplier", ExpiryRequired = true },
                new RequirementItem { Name = "Data Privacy Agreement", AppliesTo = "Supplier", ExpiryRequired = false }
            };
        }

        public static List<VerificationQueueItem> GetSampleVerificationQueue()
        {
            return new List<VerificationQueueItem>
            {
                new VerificationQueueItem { Id = 1, SupplierName = "ABC Office Supplies", DocumentName = "Safety Certificate", SubmittedTime = "Today, 9:42 AM", Status = "Pending Verification" },
                new VerificationQueueItem { Id = 2, SupplierName = "XYZ Training Services", DocumentName = "Business Permit", SubmittedTime = "Today, 10:15 AM", Status = "Pending Verification" },
                new VerificationQueueItem { Id = 3, SupplierName = "DEF Equipment Corp.", DocumentName = "Tax Clearance", SubmittedTime = "Yesterday, 4:30 PM", Status = "Pending Verification" },
                new VerificationQueueItem { Id = 4, SupplierName = "Apex Industrial Systems", DocumentName = "Safety Certificate", SubmittedTime = "Yesterday, 2:10 PM", Status = "Pending Verification" },
                new VerificationQueueItem { Id = 5, SupplierName = "Beacon Freight Logistics", DocumentName = "Data Privacy Agreement", SubmittedTime = "Sept 18, 2026", Status = "Pending Verification" }
            };
        }

        public static List<ExpiryMonitoringItem> GetSampleExpiryMonitoring()
        {
            return new List<ExpiryMonitoringItem>
            {
                new ExpiryMonitoringItem { SupplierName = "ABC Office Supplies", DocumentName = "Tax Clearance", ExpiryDate = "Oct. 15, 2026", TimeRemaining = "Expires in 30 days", Status = "Expiring Soon" },
                new ExpiryMonitoringItem { SupplierName = "XYZ Training Services", DocumentName = "Tax Clearance", ExpiryDate = "Sept. 18, 2026", TimeRemaining = "Expired 2 days ago", Status = "Expired" },
                new ExpiryMonitoringItem { SupplierName = "DEF Equipment Corp.", DocumentName = "Safety Certificate", ExpiryDate = "Sept. 10, 2026", TimeRemaining = "Expired 10 days ago", Status = "Expired" },
                new ExpiryMonitoringItem { SupplierName = "Global Logistics Inc.", DocumentName = "Business Permit", ExpiryDate = "Dec. 31, 2026", TimeRemaining = "Valid for 105 days", Status = "Valid" },
                new ExpiryMonitoringItem { SupplierName = "Apex Industrial Systems", DocumentName = "Tax Clearance", ExpiryDate = "Nov. 01, 2026", TimeRemaining = "Expires in 47 days", Status = "Expiring Soon" }
            };
        }

        public static List<ProcurementRequestItem> GetSampleProcurementRequests()
        {
            return new List<ProcurementRequestItem>
            {
                new ProcurementRequestItem { Id = 101, RequestName = "Office Materials Batch A", SupplierName = "ABC Office Supplies", RequestedBy = "Leah Ramos (Staff-HR)", Purpose = "Office materials for the department.", DateSubmitted = "Sept 20, 2026", Status = "Pending Approval", IsSupplierCompliant = true },
                new ProcurementRequestItem { Id = 102, RequestName = "Q4 Safety & Skill Training", SupplierName = "XYZ Training Services", RequestedBy = "Leah Ramos (Staff-HR)", Purpose = "Annual safety compliance workshop for staff.", DateSubmitted = "Sept 19, 2026", Status = "Pending Approval", IsSupplierCompliant = false },
                new ProcurementRequestItem { Id = 103, RequestName = "Industrial Printers & Scanners", SupplierName = "DEF Equipment Corp.", RequestedBy = "David Chen (Staff)", Purpose = "Replacement scanners for logistics hub.", DateSubmitted = "Sept 18, 2026", Status = "Pending Approval", IsSupplierCompliant = false },
                new ProcurementRequestItem { Id = 104, RequestName = "Freight Shipping Services", SupplierName = "Global Logistics Inc.", RequestedBy = "Leah Ramos (Staff-HR)", Purpose = "Inter-branch freight shipping schedule.", DateSubmitted = "Sept 15, 2026", Status = "Approved", IsSupplierCompliant = true }
            };
        }

        public static List<AuditLogItem> GetSampleAuditLogs()
        {
            return new List<AuditLogItem>
            {
                new AuditLogItem { User = "Admin", Action = "Approved document (Business Permit)", Module = "Compliance", Date = "Sept. 20, 2026 10:14 AM" },
                new AuditLogItem { User = "Supplier (ABC Office)", Action = "Uploaded document (Safety Certificate)", Module = "Compliance", Date = "Sept. 20, 2026 09:42 AM" },
                new AuditLogItem { User = "Admin", Action = "Added supplier (XYZ Training Services)", Module = "Supplier", Date = "Sept. 19, 2026 03:22 PM" },
                new AuditLogItem { User = "HR (Leah Ramos)", Action = "Created procurement request (Q4 Training)", Module = "Procurement", Date = "Sept. 19, 2026 01:15 PM" },
                new AuditLogItem { User = "Super Admin (Elena)", Action = "Created account (juan@cccorp.com)", Module = "User Management", Date = "Sept. 18, 2026 11:00 AM" },
                new AuditLogItem { User = "Super Admin (Elena)", Action = "Updated system notification settings", Module = "System Settings", Date = "Sept. 17, 2026 04:45 PM" }
            };
        }
    }
}
