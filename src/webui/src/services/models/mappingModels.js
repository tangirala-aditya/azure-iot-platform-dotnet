export const SystemDefaultMapping = {
    id: "Default",
    name: "Default",
    mapping: [
        {
            name: "Simulated",
            mapping: "isSimulated",
            cellRenderer: "IsSimulatedRenderer",
            isDefault: true,
            description: "",
        },
        {
            name: "Device type",
            mapping: "Properties.Reported.Type",
            cellRenderer: "DefaultRenderer",
            isDefault: true,
            description: "",
        },
        {
            name: "Firmware",
            mapping: "Properties.Reported.firmware.currentFwVersion",
            cellRenderer: "DefaultRenderer",
            isDefault: true,
            description: "",
        },
        {
            name: "Telemetry",
            mapping: "Properties.Reported.telemetry",
            cellRenderer: "DefaultRenderer",
            isDefault: true,
            description: "",
        },
        {
            name: "Status",
            mapping: "connected",
            cellRenderer: "ConnectionStatusRenderer",
            isDefault: true,
            description: "",
        },
        {
            name: "Last Connection",
            mapping: "lastActivity",
            cellRenderer: "TimeRenderer",
            isDefault: true,
            description: "",
        },
    ],
};
