using System.Collections.Generic;

internal static class EnvironmentDics
{
    public static readonly Dictionary<string, List<string>> TaskClientsByEnvironment =
        new Dictionary<string, List<string>>
        {
            {
                "EU1", new List<string>
                {
                    "DTAG",
                    "Austria",
                    "Czech",
                    "Netherlands",
                    "Romania",
                    "Slovakia"
                }
            },
            {
                "EU2", new List<string>
                {
                    "DTAG",
                    "Albania",
                    "Croatia",
                    "Greece",
                    "Macedonia",
                    "Montenegro"
                }
            },
            {
                "EU3", new List<string>
                {
                    "Futtaim",
                    "Technologies",
                    "Al",
                    "Al Futtaim Technologies",
                    "Express Gifts LTD",
                    "Express",
                    "Gifts",
                    "LTD",
                    "Ooredoo",
                    "Myanmar",
                    "Palestine",
                    "Omantel"
                }
            },
            {
                "EU5", new List<string>
                {
                    "AMGENERAL INSURANCE BERHAD",
                    "AMGENERAL",
                    "INSURANCE",
                    "BERHAD",
                    "Auckland City Council",
                    "Auckland",
                    "City",
                    "Council",
                    "AustralianSuper",
                    "Custom Fleet",
                    "Custom",
                    "Fleet",
                    "IAG NZ",
                    "IAG",
                    "NZ",
                    "Insurance Australia Group Limited (IAG/CGU)",
                    "IAG/CGU",
                    "IAG",
                    "CGU",
                    "Insurance Australia Group Limited",
                    "Insurance",
                    "Australia",
                    "Group",
                    "Limited",
                    "Ncell",
                    "Axiata",
                    "Nepal",
                    "Ooredoo",
                    "Royal Automobile Club of Victoria",
                    "Royal",
                    "Automobile",
                    "Club",
                    "Victoria",
                    "Swisscard",
                    "Ucell",
                    "TeliaSonera",
                    "Uzbekistan",
                    "Vodafone",
                    "Hutchison",
                    "Australia",
                    "VHA"
                }
            },
            {
                "NA3", new List<string>
                {
                    "BCLC",
                    "Delta Dental",
                    "Delta",
                    "Dental",
                    "Desjardins",
                    "Freedom Mobile Corp",
                    "Freedom",
                    "Mobile",
                    "Corp",
                    "ICBA Benefits Services",
                    "ICBA",
                    "Benefits",
                    "Services",
                    "Johnson & Johnson Patient Assistance Foundation, Inc. (JJPAF)",
                    "Johnson",
                    "Patient",
                    "Foundation",
                    "Inc",
                    "JJPAF"
                }
            },
            {
                "NA6", new List<string>
                {
                    "CAASCO"
                }
            },
            {
                "ZAIN", new List<string>
                {
                    "Zain",
                    "Bahrain",
                    "Iraq",
                    "Kuwait",
                    "Saudi Arabia",
                    "Saudi",
                    "Arabia",
                    "Sudan",
                    "KSA"
                }
            },
            {
                "SG1", new List<string>
                {
                    "OCBC"
                }
            }
        };

    public static readonly Dictionary<int, string> EnvironmentById = new Dictionary<int, string>
    {
        { 1, "EU1" },
        { 2, "EU2" },
        { 3, "EU3" },
        { 4, "EU5" },
        { 5, "NA3" },
        { 6, "NA6" },
        { 7, "ZAIN" },
        { 8, "SG1" }
    };

    public static readonly Dictionary<string, List<string>> DbCredentialsByEnv = new Dictionary<string, List<string>>
    {
        {
            "EU1", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_eu1_RT",
                "urt_eu1_faster",
                "P@ssw0rd+,RT"
            }
        },
        {
            "EU2", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_eu2_RT",
                "urt_eu2_faster",
                "P@ssw0rd+,RT"
            }
        },
        {
            "EU3", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_eu3_RT",
                "urt_eu3_faster",
                "P@ssw0rd+,RT"
            }
        },
        {
            "EU5", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_eu5_RT",
                "urt_eu5_faster",
                "P@ssw0rd+,RT"
            }
        },
        {
            "NA3", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_na3_RT",
                "urt_na3_faster",
                "P@ssw0rd+,RT"
            }
        },
        {
            "NA6", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_na6_RT",
                "urt_na6_qa",
                "P@ssw0rd+,RT"
            }
        },
        {
            "ZAIN", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_zain_RT",
                "urt_zain_faster",
                "P@ssw0rd+,RT"
            }
        },
        {
            "SG1", new List<string>
            {
                "ResponseTek_NONPROD_EU.cons.com",
                "urt_sg1_RT",
                "urt_sg1_faster",
                "P@ssw0rd+,RT"
            }
        },
    };
}