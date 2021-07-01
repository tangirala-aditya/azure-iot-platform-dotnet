// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";

import { permissions, toDiagnosticsModel } from "services/models";
import { DevicesGridContainer } from "./devicesGrid";
import { defaultDeviceColumns } from "./devicesGrid/devicesGridConfig";
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "components/shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "components/shell/manageDeviceGroupsBtn";
import { ResetActiveDeviceQueryBtnContainer as ResetActiveDeviceQueryBtn } from "components/shell/resetActiveDeviceQueryBtn";
import {
    AjaxError,
    Btn,
    ComponentArray,
    ContextMenuAgile,
    PageContent,
    PageTitle,
    Protected,
    RefreshBarContainer as RefreshBar,
    JsonEditorModal,
} from "components/shared";
import { DeviceNewContainer } from "./flyouts/deviceNew";
import { AdvanceSearchContainer } from "./advanceSearch";
import { SIMManagementContainer } from "./flyouts/SIMManagement";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";
import { svgs, getDeviceGroupParam, getTenantIdParam, translateColumnDefs } from "utilities";
import { IdentityGatewayService, IoTHubManagerService, ConfigService } from "services";
import {ColumnDialog} from "./columnDialog";
import { DefaultButton } from "@fluentui/react/lib/Button";
import { generateColumnOptionsFromMappings, generateColumnDefsFromSelectedOptions, generateSelectedOptionsFromMappings, generateColumnDefsFromMappings } from "./devicesGrid/deviceColumnHelper";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./devices.module.scss"));

const closedFlyoutState = { openFlyoutName: undefined };

const closedModalState = {
    openModalName: undefined,
};

export class Devices extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            showColumnDialog: false,
            contextBtns: null,
            selectedDeviceGroupId: undefined,
            loadMore: props.loadMoreState,
            isDeviceSearch: false,
        };
        
        this.props.updateCurrentWindow("Devices"); 

        this.DefaultColumnMappings = props.columnMappings["Default"] ? props.columnMappings["Default"].mapping : [];
        this.ColumnOptions = [];

        this.setMappingsAndOptions(props); 
        this.setColumnOptions();
    }

    setMappingsAndOptions(props, deviceGroupId = null) {
        const defaultMappings = props.columnMappings["Default"].mapping;
        const deviceGroupMappingId = props.deviceGroups.find(dg => dg.id === (deviceGroupId ?? props.activeDeviceGroupId)).mappingId;
        this.DeviceGroupColumnMappings = props.columnMappings[deviceGroupMappingId] ? defaultMappings.concat(props.columnMappings[deviceGroupMappingId].mapping) : [];
        const colOption = props.columnOptions.find(c => c.deviceGroupId === (deviceGroupId ?? props.activeDeviceGroupId));
        this.ColumnOptionsModel = colOption ?? null;
        this.SelectedOptions = colOption ? colOption.selectedOptions : [];
    }

    setColumnOptions() {
        if(this.DeviceGroupColumnMappings.length === 0 && this.SelectedOptions.length === 0 && this.DefaultColumnMappings.length > 0) {
            this.ColumnOptions = generateColumnOptionsFromMappings(this.DefaultColumnMappings);
            this.SelectedOptions = generateSelectedOptionsFromMappings(this.DefaultColumnMappings);
            this.ColumnDefinitions = generateColumnDefsFromMappings(this.DefaultColumnMappings);
        } else if(this.DeviceGroupColumnMappings.length === 0 && this.SelectedOptions.length > 0 && this.DefaultColumnMappings.length > 0) {
            this.ColumnOptions = generateColumnOptionsFromMappings(this.DefaultColumnMappings);
            this.ColumnDefinitions = generateColumnDefsFromSelectedOptions(this.DefaultColumnMappings, this.SelectedOptions);
        } else if(this.DeviceGroupColumnMappings.length > 0 && this.SelectedOptions.length === 0) {
            this.ColumnOptions = generateColumnOptionsFromMappings(this.DeviceGroupColumnMappings);
            this.SelectedOptions = generateSelectedOptionsFromMappings(this.DefaultColumnMappings);
            this.ColumnDefinitions = generateColumnDefsFromMappings(this.DefaultColumnMappings);
        } else if(this.DeviceGroupColumnMappings.length > 0 && this.SelectedOptions.length > 0) {
            this.ColumnOptions = generateColumnOptionsFromMappings(this.DeviceGroupColumnMappings);
            this.ColumnDefinitions = generateColumnDefsFromSelectedOptions(this.DeviceGroupColumnMappings, this.SelectedOptions);
        }
        
    }

    UNSAFE_componentWillMount() {
        if (this.props.location && this.props.location.search) {
            const tenantId = getTenantIdParam(this.props.location);
            this.props.checkTenantAndSwitch({
                tenantId: tenantId,
                redirectUrl: window.location.href,
            });
            this.setState({
                selectedDeviceGroupId: getDeviceGroupParam(this.props.location),
            });
        }

        if (
            this.props &&
            this.props.location &&
            this.props.location.pathname === "/deviceSearch"
        ) {
            this.props.resetDeviceByCondition();
            this.setState({
                isDeviceSearch: true,
            });
        } else {
            this.setState({
                isDeviceSearch: false,
            });
        }

        IdentityGatewayService.VerifyAndRefreshCache();
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        if (
            nextProps.isPending &&
            nextProps.isPending !== this.props.isPending
        ) {
            // If the grid data refreshes, hide most flyouts and deselect soft selections
            switch (this.state.openFlyoutName) {
                case "create-device-query":
                    // leave this flyout open even on grid refresh
                    break;
                default:
                    this.setState(closedFlyoutState);
            }
        }
    }

    componentDidMount() {
        if (this.state.selectedDeviceGroupId && this.props.location) {
            window.history.replaceState(
                {},
                document.title,
                this.props.location.pathname
            );
        }
    }

    closeFlyout = () => this.setState(closedFlyoutState);

    openSIMManagement = () =>
        this.setState({ openFlyoutName: "sim-management" });
    openNewDeviceFlyout = () => {
        this.setState({ openFlyoutName: "new-device" });
        this.props.logEvent(toDiagnosticsModel("Devices_NewClick", {}));
    };
    openCloudToDeviceFlyout = () => {
        this.setState({ openFlyoutName: "c2d-message" });
        this.props.logEvent(toDiagnosticsModel("Devices_C2DClick", {}));
    };

    onContextMenuChange = (contextBtns) =>
        this.setState({
            contextBtns,
            openFlyoutName: undefined,
        });

    openModal = (modalName, jsonValue) => {
        this.setState({
            openModalName: modalName,
            modalJson: jsonValue,
        });
    };

    closeModal = () => this.setState(closedModalState);

    openColumnOptions = () => {
        this.setState({showColumnDialog: true});
    }

    getOpenModal = () => {
        const { t, theme, logEvent } = this.props;
        if (this.state.openModalName === "json-editor") {
            return (
                <JsonEditorModal
                    t={t}
                    title={t(
                        "devices.flyouts.details.properties.editPropertyValue"
                    )}
                    onClose={this.closeModal}
                    jsonData={this.state.modalJson}
                    logEvent={logEvent}
                    theme={theme ? theme : "light"}
                />
            );
        }
        return null;
    };

    priorityChildren = () => {
        const { t } = this.props;
        const { isDeviceSearch } = this.state;

        let children = [];

        if (!isDeviceSearch) {
            children.push(
                <DeviceGroupDropdown
                    updateLoadMore={this.updateLoadMoreOnDeviceGroupChange}
                    deviceGroupIdFromUrl={this.state.selectedDeviceGroupId}
                    updateColumns={this.updateColumnsOnDeviceGroupChange}
                />,
                <Protected permission={permissions.updateDeviceGroups}>
                    <ManageDeviceGroupsBtn />
                </Protected>,
                <CreateDeviceQueryBtn />
            );
        }

        if (
            !isDeviceSearch &&
            this.props.activeDeviceQueryConditions.length !== 0
        ) {
            children.push(<ResetActiveDeviceQueryBtn />);
        }

        if (
            this.state.contextBtns &&
            this.state.contextBtns.props.children.length > 0
        ) {
            children = children.concat(this.state.contextBtns.props.children);
        }

        children.push(
            <Protected permission={permissions.updateSIMManagement}>
                <Btn svg={svgs.simmanagement} onClick={this.openSIMManagement}>
                    {t("devices.flyouts.SIMManagement.title")}
                </Btn>
            </Protected>
        );

        return children;
    };

    switchLoadMore = (value) => {
        if (!value) {
            this.setState({ loadMore: false });
            return this.props.cancelDeviceCalls({
                makeSubsequentCalls: false,
            });
        } else {
            this.setState({ loadMore: true });
            this.props.cancelDeviceCalls({
                makeSubsequentCalls: true,
            });
            return this.props.fetchDevicesByCToken();
        }
    };

    refreshDevices = () => {
        this.setState({ loadMore: false });
        this.props.cancelDeviceCalls({ makeSubsequentCalls: false });
        return this.props.fetchDevices();
    };

    updateLoadMoreOnDeviceGroupChange = () => {
        this.setState({ loadMore: false });
        this.props.cancelDeviceCalls({ makeSubsequentCalls: false });
    };

    updateColumnsOnDeviceGroupChange = (deviceGroupId) => {
        this.setMappingsAndOptions(this.props, deviceGroupId);
        this.setColumnOptions();
        this.deviceGridApi.setColumnDefs(translateColumnDefs(this.props.t, defaultDeviceColumns.concat(this.ColumnDefinitions)));
    }

    toggleColumnDialog = () => {
        const { showColumnDialog } = this.state;
        this.setState({ showColumnDialog: !showColumnDialog });
    };

    downloadFile = () => {
        IoTHubManagerService.getDevicesReportByQuery(
            this.props.activeDeviceGroupConditions
        ).subscribe((response) => {
            var blob = new Blob([response.response], {
                type: response.response.type,
            });
            let url = window.URL.createObjectURL(blob);
            let a = document.createElement("a");
            a.href = url;
            a.download = "DevicesList.xlsx";
            a.click();
        });
    };

    updateColumns = (saveUpdates, selectedColumnOptions) => {
        this.toggleColumnDialog();
        this.SelectedOptions = selectedColumnOptions;
        this.setColumnOptions();
        this.deviceGridApi.setColumnDefs(translateColumnDefs(this.props.t, defaultDeviceColumns.concat(this.ColumnDefinitions)));
        if(saveUpdates) {
            var requestData = {
                DeviceGroupId: this.props.activeDeviceGroupId,
                SelectedOptions: selectedColumnOptions,
            };
            if(!this.ColumnOptionsModel) {
                ConfigService.saveColumnOptions(requestData).subscribe(
                    (columnMapping) => {
                        this.ColumnOptionsModel = columnMapping;
                    },
                    (error) => {}
                );
            } else {
                this.ColumnOptionsModel.SelectedOptions = selectedColumnOptions;
                ConfigService.updateColumnOptions(this.ColumnOptionsModel.key, this.ColumnOptionsModel).subscribe(
                    (columnMapping) => {
                        this.ColumnOptionsModel = columnMapping;
                    },
                    (error) => {}
                );
            }
            
        }
    }

    /**
     * Get the grid api options
     *
     * @param {Object} gridReadyEvent An object containing access to the grid APIs
     */
     onGridReady = (gridReadyEvent) => {
        this.deviceGridApi = gridReadyEvent.api;
    };

    render() {
        const {
                t,
                devices,
                deviceGroupError,
                deviceError,
                isPending,
                devicesByCondition,
                devicesByConditionError,
                isDevicesByConditionPanding,
                lastUpdated,
                routeProps,
                columnMappings
            } = this.props,
            { isDeviceSearch, showColumnDialog } = this.state,
            deviceData = isDeviceSearch ? devicesByCondition : devices,
            dataError = isDeviceSearch ? devicesByConditionError : deviceError,
            isDataPending = isDeviceSearch
                ? isDevicesByConditionPanding
                : isPending,
            gridProps = {
                onGridReady: this.onGridReady,
                rowData: isDataPending ? undefined : deviceData || [],
                onContextMenuChange: this.onContextMenuChange,
                t: this.props.t,
                searchPlaceholder: this.props.t("devices.searchPlaceholder"),
                searchAreaLabel: this.props.t("devices.ariaLabel"),
            },
            newDeviceFlyoutOpen = this.state.openFlyoutName === "new-device",
            simManagementFlyoutOpen =
                this.state.openFlyoutName === "sim-management",
            error = deviceGroupError || dataError;

        return (
            <ComponentArray>
                <ContextMenuAgile
                    farChildren={[
                        <Protected permission={permissions.createDevices}>
                            {!this.state.isDeviceSearch && (
                                <Btn
                                    svg={svgs.plus}
                                    onClick={this.openNewDeviceFlyout}
                                >
                                    {t("devices.flyouts.new.contextMenuName")}
                                </Btn>
                            )}
                        </Protected>,
                        !this.state.isDeviceSearch && (
                            <RefreshBar
                                refresh={this.refreshDevices}
                                time={lastUpdated}
                                isPending={isPending}
                                t={t}
                                isShowIconOnly={true}
                            />
                        ),
                    ]}
                    priorityChildren={this.priorityChildren()}
                />
                {showColumnDialog && (<ColumnDialog
                    show={showColumnDialog}
                    toggle={this.toggleColumnDialog}
                    columnOptions={this.ColumnOptions}
                    selectedOptions={this.SelectedOptions}
                    updateColumns={this.updateColumns}
                    t={t}
                />)}
                <PageContent className={css("devices-container")}>
                    <PageTitle
                        titleValue={
                            !this.state.isDeviceSearch
                                ? t("devices.title")
                                : t("devices.deviceSearchTitle")
                        }
                        descriptionValue={
                            !this.state.isDeviceSearch
                                ? t("devices.titleDescription")
                                : t("devices.deviceSearchTitleDescription")
                        }
                    />
                    {!!error && <AjaxError t={t} error={error} />}
                    {this.state.isDeviceSearch && <AdvanceSearchContainer />}
                    {!this.state.isDeviceSearch && (
                        <div className={css("cancel-right-div")}>
                            <DefaultButton
                                iconProps={{ iconName: "Download" }}
                                onClick={this.downloadFile}
                                text={t("devices.downloadDeviceReport")}
                            />

                            <DefaultButton
                                iconProps={{ iconName: "ColumnOptions" }}
                                onClick={this.openColumnOptions}
                                text={t("devices.columnOptions")}
                            />
                            <Toggle
                                attr={{
                                    button: {
                                        "aria-label": t("devices.loadMore"),
                                        type: "button",
                                    },
                                }}
                                className="TODO-AddClassToPositionControl"
                                on={this.state.loadMore}
                                onLabel={t("devices.loadMore")}
                                offLabel={t("devices.loadMore")}
                                onChange={this.switchLoadMore}
                            />
                        </div>
                    )}
                    {!error && (
                        <DevicesGridContainer
                            {...gridProps}
                            {...routeProps}
                            openPropertyEditorModal={this.openModal}
                            deviceColumnMappings={columnMappings}
                            columnDefs={this.ColumnDefinitions}
                        />
                    )}
                    {newDeviceFlyoutOpen && (
                        <DeviceNewContainer onClose={this.closeFlyout} />
                    )}
                    {simManagementFlyoutOpen && (
                        <SIMManagementContainer onClose={this.closeFlyout} />
                    )}
                </PageContent>
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
