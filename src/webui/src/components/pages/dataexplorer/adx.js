// Copyright (c) Microsoft. All rights reserved.

import React, { Component, Fragment } from "react";
import { IdentityGatewayService } from "services";

const closedFlyoutState = { openFlyoutName: undefined };

export class ADX extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
        };
    }

    componentDidMount() {
        console.log("ADX-Mount");
        // window.parent.postMessage(
        //     {
        //         signature: "queryExplorer",
        //         type: "getToken",
        //     },
        //     "*"
        // );
        window.addEventListener(
            "message",
            (event) => this.handleIncomingMessage(event),
            false
        );
    }

    fetchToken() {
        IdentityGatewayService.getAccessTokenForADX().subscribe((value) => {
            const iframe = document.getElementById("ADXUI");
            iframe.contentWindow.postMessage(
                {
                    type: "postToken",
                    message: value,
                },
                "*"
            );
        });
    }

    handleIncomingMessage(event) {
        if (event.origin === "https://dataexplorer.azure.com") {
            console.log(event);
            this.fetchToken();
            let intervalId = setInterval(this.fetchToken, 3600000);
            this.setState({ intervalId: intervalId });
        }
        return true;
    }

    UNSAFE_componentWillMount() {
        IdentityGatewayService.VerifyAndRefreshCache();
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        if (
            nextProps.isPending &&
            nextProps.isPending !== this.props.isPending
        ) {
            // If the grid data refreshes, hide the flyout and deselect soft selections
            this.setState(closedFlyoutState);
        }
    }

    UNSAFE_componentWillUnmount() {
        clearInterval(this.state.intervalId);
    }

    render() {
        return (
            <Fragment>
                <iframe
                    id="ADXUI"
                    title="ADX Dashboard"
                    height="400px"
                    src="https://dataexplorer.azure.com/clusters/acsagickustodev.centralus.kusto.windows.net?ibizaPortal=true&&ShowConnectionButtons=true&&IFrameAuth=true"
                ></iframe>
            </Fragment>
        );
    }
}
