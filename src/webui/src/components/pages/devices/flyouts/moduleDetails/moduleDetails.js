// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { IoTHubManagerService } from "services";
import { LinkedComponent, copyToClipboard, svgs } from "utilities";
import {
    Btn,
    PropertyGrid as Grid,
    PropertyRow as Row,
    PropertyCell as Cell,
    FormControl,
} from "components/shared";
import Flyout from "components/shared/flyout";

export class ModuleDetails extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            edgeModuleLogs: undefined,
            edgeModuleLogsJson: {
                jsObject: {},
            },
        };
        this.baseState = this.state;
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentDidMount() {
        this.fetchModuleLogs();
    }

    componentWillUnmount() {}

    copyDevicePropertiesToClipboard = () => {
        if (this.props.device) {
            copyToClipboard(JSON.stringify(this.props.device.properties || {}));
        }
    };

    toggleRawDiagnosticsMessage = () => {
        this.setState({ showRawMessage: !this.state.showRawMessage });
    };

    applyRuleNames = (alerts, rules) =>
        alerts.map((alert) => ({
            ...alert,
            name: (rules[alert.ruleId] || {}).name,
        }));

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
        });
    };

    fetchModuleLogs = (deviceId, moduleId) => {
        IoTHubManagerService.getModuleLogs(deviceId, moduleId).subscribe(
            (response) => {
                debugger;
                console.log(response);
                this.setState({
                    edgeModuleLogs: response,
                    edgeModuleLogsJson: {
                        jsObject: response,
                    },
                });
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

    expandFlyout() {
        if (this.state.expandedValue) {
            this.setState({
                expandedValue: false,
            });
        } else {
            this.setState({
                expandedValue: true,
            });
        }
    }

    render() {
        const { t, onClose, flyoutLink, moduleId, theme } = this.props;
        //const { edgeModuleLogs } = this.state;
        this.logJsonLink = this.linkTo("edgeModuleLogsJson");

        return (
            <Flyout.Container
                header={t("devices.flyouts.details.title")}
                t={t}
                onClose={onClose}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
                flyoutLink={flyoutLink}
            >
                <Grid>
                    <Row>
                        <Cell className="col-8">
                            <Btn
                                svg={svgs.refresh}
                                onClick={this.copyDevicePropertiesToClipboard}
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
                                onClick={this.copyDevicePropertiesToClipboard}
                            >
                                {t(
                                    "devices.flyouts.details.moduleDetails.download"
                                )}
                            </Btn>
                        </Cell>
                    </Row>
                </Grid>
                <div>
                    <form>
                        <FormControl
                            link={this.logJsonLink}
                            type="jsoninput"
                            height="100%"
                            theme={theme}
                        />
                    </form>
                </div>
            </Flyout.Container>
        );
    }
}
