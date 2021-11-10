// Copyright (c) Microsoft. All rights reserved.

import React, { Component, Fragment } from "react";
// import {
//     Panel,
//     PanelContent,
//     PanelHeader,
//     PanelHeaderLabel,
// } from "components/pages/dashboard/panel";
// const classnames = require("classnames/bind");
// const css = classnames.bind(require("./edgeDeviceDetails.module.scss"));
export const getIntervalParams = (timeInterval) => {
    switch (timeInterval) {
        case "PT15M":
            return "now-15m";
        case "P1D":
            return "now-1d";
        case "P7D":
            return "now-7d";
        case "P1M":
            return "now-1M";
        default:
            // Use PT1H as the default case
            return "now-1h";
    }
};

export class EdgeDeviceInfoDashboard extends Component {
    constructor(props) {
        super(props);
        this.state = { deviceId: props.deviceId, from: "now-1h" };
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        this.prepareUrl(nextProps);
    }

    prepareUrl(props) {
        this.setState({ from: getIntervalParams(props.timeInterval) });
        this.setState({ deviceGroupId: (props.deviceId || {}).id });
    }

    render() {
        // const { t } = this.props;
        return (
            // <Panel>
            //     <PanelHeader>
            //         <PanelHeaderLabel>
            //             {t("dashboard.panels.dashboard.header")}
            //         </PanelHeaderLabel>
            //     </PanelHeader>
            //     <PanelContent className={css("telemetry-panel-container")}>
            <Fragment>
                <iframe
                    title="Dashboard"
                    src={
                        "http://localhost:3000/grafana/d/SsCPi9v7z/edgedashboard?orgId=50&theme=light&refresh=10s&kiosk"
                    }
                    width="100%"
                    height="100%"
                    frameborder="0"
                ></iframe>
            </Fragment>
            //     </PanelContent>
            // </Panel>
        );
    }
}
