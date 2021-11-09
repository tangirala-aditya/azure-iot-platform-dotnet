// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { IoTHubManagerService } from "services";
import { LinkedComponent, copyToClipboard, svgs } from "utilities";
import {
    ComponentArray,
    Btn,
    ContextMenu,
    ContextMenuAlign,
    PropertyGrid as Grid,
    PropertyGridBody as GridBody,
    PropertyGridHeader as GridHeader,
    PropertyRow as Row,
    PropertyCell as Cell,
    // FormControl,
    AjaxError,
    SectionDesc,
} from "components/shared";
import Flyout from "components/shared/flyout";
const classnames = require("classnames/bind");
const css = classnames.bind(
    require("../deviceDetails/deviceDetails.module.scss")
);

const Section = Flyout.Section;

export class ModuleDetails extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            edgeModuleLogs: undefined,
            edgeModuleLogsJson: {
                jsObject: {},
            },
            error: undefined,
            deviceId: props.match.params.deviceId,
            moduleId: props.match.params.moduleId,
        };
        this.baseState = this.state;
    }

    componentDidMount() {
        this.fetchModuleLogs(this.state.deviceId, this.state.moduleId);
    }

    componentWillUnmount() {}

    copyDevicePropertiesToClipboard = () => {
        if (this.props.device) {
            copyToClipboard(JSON.stringify(this.props.device.properties || {}));
        }
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
                    },
                    (error) => {
                        this.setState({
                            error: error,
                        });
                    }
                );
                // var data =
                //     "text/json;charset=utf-8," +
                //     encodeURIComponent(JSON.stringify(response.response));
                // //var blob = new Blob([response.response], {
                // //    type: response.response.contentType,
                // //});
                // //let url = window.URL.createObjectURL(blob);
                // debugger;
                // let a = document.createElement("a");
                // a.href = "data:" + data;
                // a.download = moduleId + "DeviceLogs.json";
                // a.click();
            }
        );
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
        var blob = new Blob(this.state.edgeModuleLogs, {
            type: "application/json",
        });
        let url = window.URL.createObjectURL(blob);
        let a = document.createElement("a");
        // a.href = "data:" + data;
        a.href = url;
        a.download = "DeviceLogs.json";
        a.click();
    };

    render() {
        const { t, moduleId } = this.props;
        const { error } = this.state,
            edgeModuleLogs = this.state.edgeModuleLogs || [];
        this.logJsonLink = this.linkTo("edgeModuleLogsJson");

        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={true}>
                        <Btn svg={svgs.return} onClick={this.navigateToDevices}>
                            {t("devices.returnToDevices")}
                        </Btn>
                    </ContextMenuAlign>
                    {/* <ContextMenuAlign right={true}>
                        <TimeIntervalDropdown
                            onChange={this.updateTimeInterval}
                            value={this.props.timeInterval}
                            t={t}
                        />
                        <RefreshBar
                            refresh={this.refreshTelemetry}
                            time={lastRefreshed}
                            t={t}
                            isShowIconOnly={true}
                        />
                    </ContextMenuAlign> */}
                </ContextMenu>
                <div>
                    <Section.Container>
                        <Section.Header>
                            {t("devices.flyouts.details.moduleDetails.title")}
                        </Section.Header>
                        <Section.Content>
                            <SectionDesc></SectionDesc>
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
                                                onClick={
                                                    this
                                                        .copyDevicePropertiesToClipboard
                                                }
                                            >
                                                {t(
                                                    "devices.flyouts.details.moduleDetails.restart"
                                                )}
                                                {moduleId}
                                            </Btn>
                                        </Cell>
                                        <Cell className="col-2">
                                            <Btn
                                                svg={svgs.upload}
                                                className={css(
                                                    "download-deviceupload"
                                                )}
                                                onClick={
                                                    this.downloadModuleLogs
                                                }
                                            >
                                                {t(
                                                    "devices.flyouts.details.moduleDetails.download"
                                                )}
                                            </Btn>
                                        </Cell>
                                    </Row>
                                </Grid>
                                {edgeModuleLogs.length === 0 &&
                                    t(
                                        "devices.flyouts.details.moduleDetails.noneExist"
                                    )}
                                {edgeModuleLogs.length > 0 && (
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
                        </Section.Content>
                    </Section.Container>
                    {error && (
                        <AjaxError
                            className="devices-new-error"
                            t={t}
                            error={error}
                        />
                    )}
                </div>
            </ComponentArray>
        );
    }
}
