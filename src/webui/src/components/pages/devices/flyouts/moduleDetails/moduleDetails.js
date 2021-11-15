// Copyright (c) Microsoft. All rights reserved.

import React, { Fragment } from "react";
import { IoTHubManagerService } from "services";
import { LinkedComponent, svgs } from "utilities";
import {
    ComponentArray,
    Btn,
    PropertyGrid as Grid,
    PropertyGridBody as GridBody,
    PropertyGridHeader as GridHeader,
    PropertyRow as Row,
    PropertyCell as Cell,
    AjaxError,
    FormGroup,
    FormControl,
    Modal,
    BtnToolbar,
} from "components/shared";
const classnames = require("classnames/bind");
const css = classnames.bind(
    require("../deviceDetails/deviceDetails.module.scss")
);
const modulecss = classnames.bind(require("./moduleDetails.module.scss"));

const closedModalState = {
    openModalName: undefined,
    flyoutLink: undefined,
};

export class ModuleDetails extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            edgeModuleLogsFetchPending: true,
            edgeModuleLogs: undefined,
            edgeModuleLogsJson: {
                jsObject: {},
            },
            edgeDeviceStatus: 200,
            edgeDeviceStatusPending: false,
            error: undefined,
            restartModulesIsPending: true,
            deviceId: props.deviceId,
            moduleId: props.moduleId,
            formData: {
                selectedModuleId: props.moduleId,
            },
            openModalName: "",
            flyoutLink: "",
        };
        this.baseState = this.state;
        this.formDataLink = this.linkTo("formData");
        this.moduleIdLink = this.formDataLink.forkTo("selectedModuleId");
    }

    componentDidMount() {
        this.fetchEdgeModules(this.state.deviceId);
    }

    restartEdgeModule = () => {
        IoTHubManagerService.restartSelectedEdgeModule(
            this.state.deviceId,
            this.state.moduleId
        ).subscribe((response) => {
            this.setState({
                openModalName: "restart-message",
                flyoutLink: "restarted successfully",
            });
        });
    };

    fetchEdgeModules = (deviceId) => {
        IoTHubManagerService.getEdgeModules(deviceId).subscribe((modules) => {
            var edgeModules = [];
            modules.items.forEach((module) => {
                if (module) {
                    edgeModules.push(module);
                }
            });
            this.setState({
                edgeModules: edgeModules,
            });
            this.fetchModuleLogs(this.state.deviceId, this.state.moduleId);
        });
    };

    fetchModuleLogs = (deviceId, moduleId) => {
        IoTHubManagerService.getModuleLogs(deviceId, moduleId).subscribe(
            (response) => {
                this.setState(
                    {
                        edgeModuleLogs: response,
                        edgeModuleLogsJson: {
                            jsObject: response,
                        },
                        edgeModuleLogsFetchPending: false,
                    },
                    (error) => {
                        this.setState({
                            error: error,
                        });
                    }
                );
            }
        );
    };

    navigateToDevices = () => {
        this.props.history.push("/devices");
    };

    downloadModuleLogs = () => {
        var blob = new Blob([JSON.stringify(this.state.edgeModuleLogs)], {
            type: "application/json",
        });
        let url = window.URL.createObjectURL(blob);
        let a = document.createElement("a");
        a.href = url;
        a.download = "DeviceLogs.json";
        a.click();
    };

    onModuleSelected = (e) => {
        this.fetchModuleLogs(this.state.deviceId, e.target.value.value);
        this.setState({
            moduleId: e.target.value.value,
            formData: {
                selectedModuleId: e.target.value.value,
            },
        });
    };

    openModal = (message) => () => {
        debugger;
        this.setState({
            openModalName: "restart-message",
            flyoutLink: message,
        });
    };
    closeModal = () => this.setState(closedModalState);

    getOpenModal = () => {
        const { t } = this.props;
        if (this.state.openModalName === "restart-message") {
            return (
                <Modal
                    className={modulecss("copy-modal-container")}
                    onClose={() => this.closeModal()}
                >
                    <div className={modulecss("copy-header-container")}>
                        <div className={modulecss("copy-title")}>
                            {"Message"}
                        </div>
                    </div>
                    <div className={modulecss("copy-info")}>
                        {this.state.flyoutLink}
                    </div>
                    <div className={modulecss("copy-summary")}>
                        <BtnToolbar>
                            <Btn
                                svg={svgs.cancelX}
                                onClick={() => this.closeModal()}
                            >
                                {t("modal.cancel")}
                            </Btn>
                        </BtnToolbar>
                    </div>
                </Modal>
            );
        }
        return null;
    };

    render() {
        const { t } = this.props;
        const { moduleId } = this.state;
        const { error } = this.state,
            edgeModuleLogs = this.state.edgeModuleLogs || [],
            edgeModuleLogsFetchPending = this.state.edgeModuleLogsFetchPending;
        const moduleOptions = this.state.edgeModules
            ? this.state.edgeModules.map((module) => ({
                  label: module.moduleId,
                  value: module.moduleId,
              }))
            : [];

        return (
            <ComponentArray>
                <Fragment>
                    <FormGroup className={modulecss("moduledropdown")}>
                        <FormControl
                            name="selectedModuleId"
                            link={this.moduleIdLink}
                            ariaLabel={t("users.flyouts.new.systemAdmin.label")}
                            type="select"
                            options={moduleOptions}
                            placeholder={t(
                                "devices.flyouts.details.moduleDetails.modulePlaceHolder"
                            )}
                            onChange={(e) => this.onModuleSelected(e)}
                        />
                    </FormGroup>
                    {this.state.moduleId && (
                        <div
                            className={css(
                                "device-details-deviceDeployments-contentbox"
                            )}
                        >
                            <Grid>
                                <Row>
                                    <Cell className="col-8"></Cell>
                                    <Cell className="col-2">
                                        <Btn
                                            svg={svgs.refresh}
                                            onClick={this.restartEdgeModule}
                                        >
                                            {t(
                                                "devices.flyouts.details.moduleDetails.restart"
                                            )}
                                            {moduleId}
                                        </Btn>
                                    </Cell>
                                    <Cell className="col-2">
                                        <Btn
                                            icon={"download"}
                                            className={css(
                                                "download-deviceupload"
                                            )}
                                            onClick={this.downloadModuleLogs}
                                        >
                                            {t(
                                                "devices.flyouts.details.moduleDetails.download"
                                            )}
                                        </Btn>
                                    </Cell>
                                </Row>
                            </Grid>
                            {edgeModuleLogsFetchPending &&
                                t("Fetching Module Logs")}
                            {!edgeModuleLogsFetchPending &&
                                edgeModuleLogs.length === 0 &&
                                t(
                                    "devices.flyouts.details.moduleDetails.noneExist"
                                )}
                            {!edgeModuleLogsFetchPending &&
                                edgeModuleLogs.length > 0 && (
                                    <Grid
                                        className={css(
                                            "device-details-deviceDeployments"
                                        )}
                                    >
                                        <GridHeader>
                                            <Row>
                                                <Cell className="col-9">
                                                    {t(
                                                        "devices.flyouts.details.moduleDetails.log"
                                                    )}
                                                </Cell>
                                                <Cell className="col-3">
                                                    {t(
                                                        "devices.flyouts.details.moduleDetails.timeStamp"
                                                    )}
                                                </Cell>
                                            </Row>
                                        </GridHeader>
                                        <GridBody>
                                            {edgeModuleLogs.map(
                                                (edgeModuleLog, idx) => (
                                                    <Row key={idx}>
                                                        <Cell className="col-9">
                                                            {edgeModuleLog.text}
                                                        </Cell>
                                                        <Cell className="col-3">
                                                            {
                                                                edgeModuleLog.timestamp
                                                            }
                                                        </Cell>
                                                    </Row>
                                                )
                                            )}
                                        </GridBody>
                                    </Grid>
                                )}
                        </div>
                    )}
                    {error && (
                        <AjaxError
                            className="devices-new-error"
                            t={t}
                            error={error}
                        />
                    )}
                </Fragment>
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
