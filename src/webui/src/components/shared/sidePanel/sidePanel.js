// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { joinClasses } from "utilities";
import { Btn } from "components/shared";
// import { toDiagnosticsModel } from "services/models";

import "./sidePanel.scss";

export class SidePanel extends Component {
    render() {
        const { isExpanded, onClick, children, titleName } = this.props;
        return (
            <div
                className={joinClasses(
                    "side-panel",
                    isExpanded && "expanded"
                )}
            >
                <Btn
                    icon={isExpanded ? "chevronLeft" : "chevronRight"}
                    title={isExpanded ? "Collapse" : "Expand"}
                    className="side-panel-button"
                    onClick={onClick}
                />
                {isExpanded && (
                    <div className="side-panel-container">
                        {titleName && (
                            <h1 title={titleName} className="side-panel-header">
                                {titleName}
                            </h1>
                        )}
                        <div className="scrollable"> {children} </div>
                    </div>
                )}
            </div>
        );
    }
}
