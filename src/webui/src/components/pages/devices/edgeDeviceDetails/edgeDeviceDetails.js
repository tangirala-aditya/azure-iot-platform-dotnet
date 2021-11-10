// Copyright (c) Microsoft. All rights reserved.

import React, { Fragment } from "react";
import { IoTHubManagerService } from "services";
import { NavLink } from "react-router-dom";
import { Route, Switch } from "react-router-dom";
import { LinkedComponent, copyToClipboard, svgs } from "utilities";
import {
    ComponentArray,
    Btn,
    ContextMenu,
    ContextMenuAlign,
    // FormControl,
    AjaxError,
    PageContent,
} from "components/shared";
import { ModuleDetailsContainer } from "../flyouts/moduleDetails/moduleDetails.container";
import { EdgeDeviceInfoDashboard } from "./edgeDeviceInfoDashboard";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./edgeDeviceDetails.module.scss"));
const maintenanceCss = classnames.bind(
    require("./edgeDeviceDetails.module.scss")
);

export class EdgeDeviceDetails extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            edgeModules: undefined,
            edgeModuleLogsFetchPending: true,
            edgeModuleLogs: undefined,
            edgeModuleLogsJson: {
                jsObject: {},
            },
            edgeDeviceStatus: 404,
            edgeDeviceStatusPending: true,
            error: undefined,
            deviceId: props.match.params.deviceId,
            moduleId: props.match.params.moduleId,
            formData: {
                selectedModuleId: "",
            },
        };
        this.baseState = this.state;

        this.formDataLink = this.linkTo("formData");
        this.moduleIdLink = this.formDataLink.forkTo("selectedModuleId");
    }

    componentDidMount() {
        this.fetchEdgeDeviceStatus(this.state.deviceId);
    }

    copyDevicePropertiesToClipboard = () => {
        if (this.props.device) {
            copyToClipboard(JSON.stringify(this.props.device.properties || {}));
        }
    };

    // fetchModuleLogs = (deviceId, moduleId) => {
    //     IoTHubManagerService.getModuleLogs(deviceId, moduleId).subscribe(
    //         (response) => {
    //             this.setState(
    //                 {
    //                     edgeModuleLogs: response,
    //                     edgeModuleLogsJson: {
    //                         jsObject: response,
    //                     },
    //                     edgeModuleLogsFetchPending: false,
    //                 },
    //                 (error) => {
    //                     this.setState({
    //                         error: error,
    //                     });
    //                 }
    //             );
    //             // var data =
    //             //     "text/json;charset=utf-8," +
    //             //     encodeURIComponent(JSON.stringify(response.response));
    //             // //var blob = new Blob([response.response], {
    //             // //    type: response.response.contentType,
    //             // //});
    //             // //let url = window.URL.createObjectURL(blob);
    //             // debugger;
    //             // let a = document.createElement("a");
    //             // a.href = "data:" + data;
    //             // a.download = moduleId + "DeviceLogs.json";
    //             // a.click();
    //         }
    //     );
    // };

    fetchEdgeDeviceStatus = (deviceId) => {
        IoTHubManagerService.getEdgeDeviceStatus(deviceId).subscribe(
            (response) => {
                console.log(response);
                this.setState(
                    {
                        edgeDeviceStatus: response.status,
                        edgeDeviceStatusPending: false,
                    },
                    (error) => {
                        this.setState({
                            error: error,
                        });
                    }
                );
                if (
                    !this.state.edgeDeviceStatusPending &&
                    this.state.edgeDeviceStatus === 200
                ) {
                    this.fetchEdgeModules(this.state.deviceId);
                }
            }
        );
    };

    fetchEdgeModules = (deviceId) => {
        IoTHubManagerService.getEdgeModules(deviceId).subscribe((modules) => {
            var edgeModules = [];
            modules.items.forEach((module) => {
                if (module) {
                    edgeModules.push(module);
                }
            });
            console.log(edgeModules);
            this.setState({
                edgeModules: edgeModules,
            });
        });
    };

    navigateToDevices = () => {
        this.props.history.push("/devices");
    };

    downloadModuleLogs = () => {
        debugger;
        console.log(this.state.edgeModuleLogs);
        // var data =
        //     "text/json;charset=utf-8," +
        //     encodeURIComponent(JSON.stringify(edgeModuleLogs));
        var blob = new Blob([JSON.stringify(this.state.edgeModuleLogs)], {
            type: "application/json",
        });
        let url = window.URL.createObjectURL(blob);
        let a = document.createElement("a");
        a.href = url;
        a.download = "DeviceLogs.json";
        a.click();
    };

    onSystemAdminSelected = (e) => {
        console.log(this.state.formData.selectedModuleId);
        this.setState({
            moduleId: e.target.value.value,
            formData: {
                selectedModuleId: e.target.value.value,
            },
        });
    };

    render() {
        const { t } = this.props;
        const { error } = this.state;
        const isEdgeDeviceActive =
                this.state.edgeDeviceStatus === 200 ? true : false,
            edgeDeviceStatusPending = this.state.edgeDeviceStatusPending;
        // const isEdgeDeviceActive = true;
        // const edgeDeviceStatusPending = false;
        this.logJsonLink = this.linkTo("edgeModuleLogsJson");

        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={true}>
                        <Btn svg={svgs.return} onClick={this.navigateToDevices}>
                            {t("devices.returnToDevices")}
                        </Btn>
                    </ContextMenuAlign>
                </ContextMenu>
                {!isEdgeDeviceActive && !edgeDeviceStatusPending && (
                    <center>
                        <h1>Device is Offline</h1>
                        <h2>Please try after sometime.</h2>
                    </center>
                )}
                {isEdgeDeviceActive && (
                    <Fragment>
                        <PageContent
                            className={`${maintenanceCss(
                                "maintenance-container"
                            )}  ${css("summary-container")}`}
                        >
                            <div className={maintenanceCss("tab-container")}>
                                <NavLink
                                    to={`/devices/modulelogs/${this.state.deviceId}`}
                                    className={maintenanceCss("tab")}
                                    activeClassName={maintenanceCss("active")}
                                >
                                    {this.props.t(
                                        "edgeDeviceDetails.moduleInfo"
                                    )}
                                </NavLink>
                                <NavLink
                                    to={`/devices/deviceinfo/${this.state.deviceId}`}
                                    className={maintenanceCss("tab")}
                                    activeClassName={maintenanceCss("active")}
                                >
                                    {this.props.t(
                                        "edgeDeviceDetails.deviceInfo"
                                    )}
                                </NavLink>
                            </div>
                            <div className={css("grid-container")}>
                                <Switch>
                                    <Route
                                        exact
                                        path={"/devices/modulelogs/:deviceId"}
                                        render={() => (
                                            <div>
                                                <ModuleDetailsContainer
                                                    deviceId={
                                                        this.state.deviceId
                                                    }
                                                    moduleId={
                                                        this.state.moduleId
                                                    }
                                                />
                                            </div>
                                        )}
                                    />
                                    <Route
                                        exact
                                        path={"/devices/deviceinfo/:deviceId"}
                                        render={() => (
                                            <EdgeDeviceInfoDashboard
                                                deviceId={this.state.deviceId}
                                                t={this.props.t}
                                            />
                                        )}
                                    />
                                </Switch>
                            </div>
                        </PageContent>
                        {error && (
                            <AjaxError
                                className="devices-new-error"
                                t={t}
                                error={error}
                            />
                        )}
                    </Fragment>
                )}
            </ComponentArray>
        );
    }
}
