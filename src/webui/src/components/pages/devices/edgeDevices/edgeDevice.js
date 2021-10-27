// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";

import { permissions, toDiagnosticsModel } from "services/models";
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
import { DeviceNewContainer } from "../flyouts/deviceNew";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";
import { svgs, getDeviceGroupParam, getTenantIdParam } from "utilities";

import { IdentityGatewayService, IoTHubManagerService } from "services";
import { EdgeDevicesGridContainer } from "../edgeDeviceGrid/edgeDeviceGrid.container";

const closedFlyoutState = { openFlyoutName: undefined };

const closedModalState = {
    openModalName: undefined,
};

export class EdgeDevice extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
            selectedDeviceGroupId: undefined,
            loadMore: props.loadMoreState,
        };

        this.props.updateCurrentWindow("Devices");
    }

    componentWillMount() {
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

        IdentityGatewayService.VerifyAndRefreshCache();
    }

    componentWillReceiveProps(nextProps) {
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

    openNewDeviceFlyout = () => {
        this.setState({ openFlyoutName: "new-device" });
        this.props.logEvent(toDiagnosticsModel("Devices_NewClick", {}));
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

        let children = [];

        children.push(
            <DeviceGroupDropdown
                updateLoadMore={this.updateLoadMoreOnDeviceGroupChange}
                deviceGroupIdFromUrl={this.state.selectedDeviceGroupId}
            />,
            <Protected permission={permissions.updateDeviceGroups}>
                <ManageDeviceGroupsBtn />
            </Protected>,
            <CreateDeviceQueryBtn />
        );

        if (this.props.activeDeviceQueryConditions.length !== 0) {
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

    render() {
        const {
                t,
                devices,
                deviceGroupError,
                deviceError,
                isPending,
                lastUpdated,
                routeProps,
            } = this.props,
            deviceData = devices,
            dataError = deviceError,
            isDataPending = isPending,
            gridProps = {
                onGridReady: this.onGridReady,
                rowData: isDataPending ? undefined : deviceData || [],
                onContextMenuChange: this.onContextMenuChange,
                t: this.props.t,
                searchPlaceholder: this.props.t("devices.searchPlaceholder"),
                searchAreaLabel: this.props.t("devices.ariaLabel"),
            },
            newDeviceFlyoutOpen = this.state.openFlyoutName === "new-device",
            error = deviceGroupError || dataError;

        return (
            <ComponentArray>
                <ContextMenuAgile
                    farChildren={[
                        <Protected permission={permissions.createDevices}>
                            {
                                <Btn
                                    svg={svgs.plus}
                                    onClick={this.openNewDeviceFlyout}
                                >
                                    {t("devices.flyouts.new.contextMenuName")}
                                </Btn>
                            }
                        </Protected>,
                        <RefreshBar
                            refresh={this.refreshDevices}
                            time={lastUpdated}
                            isPending={isPending}
                            t={t}
                            isShowIconOnly={true}
                        />,
                    ]}
                    priorityChildren={this.priorityChildren()}
                />
                <PageContent className="devices-container">
                    <PageTitle
                        titleValue={t("devices.edgeDeviceExplorerTitle")}
                        descriptionValue={t(
                            "devices.edgeDeviceExplorerDescription"
                        )}
                    />
                    {!!error && <AjaxError t={t} error={error} />}
                    {
                        <div className="cancel-right-div">
                            <Toggle
                                attr={{
                                    button: {
                                        "aria-label": t("devices.loadMore"),
                                        type: "button",
                                    },
                                }}
                                on={this.state.loadMore}
                                onLabel={t("devices.loadMore")}
                                offLabel={t("devices.loadMore")}
                                onChange={this.switchLoadMore}
                            />
                            <Btn
                                svg={svgs.upload}
                                className="download-deviceReport"
                                onClick={this.downloadFile}
                            >
                                {t("devices.downloadDeviceReport")}
                            </Btn>
                        </div>
                    }
                    {!error && (
                        <EdgeDevicesGridContainer
                            {...gridProps}
                            {...routeProps}
                            openPropertyEditorModal={this.openModal}
                        />
                    )}
                    {newDeviceFlyoutOpen && (
                        <DeviceNewContainer onClose={this.closeFlyout} />
                    )}
                </PageContent>
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
