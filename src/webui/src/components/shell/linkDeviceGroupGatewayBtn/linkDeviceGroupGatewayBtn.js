// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { Btn } from "components/shared";
import { svgs } from "utilities";
import { toDiagnosticsModel } from "services/models";

export class LinkDeviceGroupGatewayBtn extends Component {
    onClick = () => {
        console.log(this.props);
        this.props.logEvent(
            toDiagnosticsModel("LinkDeviceGroupGateway_Click", {})
        );
        this.setState({ openFlyoutName: "link-devicegroup-gateway" });
        this.props.openFlyout();
    };

    render() {
        return (
            <Btn svg={svgs.copyLink} onClick={this.onClick}>
                {this.props.t("linkDeviceGroupGateway.title")}
            </Btn>
        );
    }
}
