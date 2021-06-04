// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";



export default class Dashboard extends Component {
    constructor(props) {
        super(props);

        //this.state = initialState;

        this.subscriptions = [];
       // this.props.updateCurrentWindow("Dashboard");
    }

    render() {
       
        const styleFrame = {
            width:"98%",
            height:"98%",
            frameBorder:0
        }
        return (
                <div style={styleFrame}>
                    <iframe title="ADX Dashboard" style={{width:"98%",height:"98%", frameBorder:0, border:0}}
  src="https://dataexplorer.azure.com/dashboards/5ed18353-780c-456f-8756-19caa6df15ed#45679f50-f0bc-423f-b929-bbbf193647e0"
></iframe>
                </div>
        );
    }
}
