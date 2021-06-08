// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { ADX } from "./adx.js";

const closedFlyoutState = { openFlyoutName: undefined };

export class DataExplorer extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
        };
    }

    componentDidMount() {
        console.log("user-Mount");
        // window.addEventListener(
        //     "getToken",
        //     (event) => this.handleIncomingMessage(event),
        //     false
        // );
    }
    render() {
        return <ADX></ADX>;
    }
}
