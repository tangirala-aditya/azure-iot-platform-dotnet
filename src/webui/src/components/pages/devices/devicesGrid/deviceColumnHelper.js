import { toPascalCase } from "utilities";

export const generateColumnDefsFromMappings = (mappings = []) => {
    let columnDefs = [];
    if (mappings.length > 0) {
        mappings.forEach((mapping) => {
            columnDefs.push({
                headerName: mapping.name,
                field: mapping.name,
                cellRendererFramework: mapping.cellRenderer,
            });
        });
    }
    return columnDefs;
};

export const generateColumnDefsFromSelectedOptions = (
    mappings = [],
    selectedOptions = []
) => {
    let columnDefs = [];
    if (mappings.length > 0 && selectedOptions.length > 0) {
        selectedOptions.forEach((option) => {
            if (mappings.find((m) => m.name === option)) {
                let mapping = mappings.filter((m) => m.name === option)[0];
                columnDefs.push({
                    headerName: mapping.name,
                    field: mapping.name,
                    cellRendererFramework: mapping.cellRenderer,
                });
            }
        });
    }
    return columnDefs;
};

export const generateColumnOptionsFromMappings = (mappings = []) => {
    let columnOptions = [];
    if (mappings.length > 0) {
        mappings.forEach((element) => {
            columnOptions.push({
                label: element.name,
                value: element.name,
            });
        });
    }
    return columnOptions;
};

export const generateSelectedOptionsFromMappings = (mappings = []) => {
    let selectedOptions = [];
    if (mappings.length > 0) {
        mappings.forEach((element) => {
            selectedOptions.push(element.name);
        });
    }
    return selectedOptions;
};

export const generateMappingObjectForDownload = (
    mappings = [],
    selectedOptions = []
) => {
    let columnDefs = [
        {
            name: "Device Name",
            mapping: "Id",
        },
    ];
    if (mappings.length > 0 && selectedOptions.length > 0) {
        selectedOptions.forEach((option) => {
            if (mappings.find((m) => m.name === option)) {
                let mapping = mappings.filter((m) => m.name === option)[0];
                columnDefs.push({
                    Name: mapping.name,
                    Mapping: mapping.mapping.includes(".")
                        ? mapping.mapping
                        : toPascalCase(mapping.mapping),
                });
            }
        });
    }
    return columnDefs;
};
