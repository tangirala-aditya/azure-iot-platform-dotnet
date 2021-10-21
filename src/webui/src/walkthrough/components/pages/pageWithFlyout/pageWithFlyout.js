// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import {
    Btn,
    ComponentArray,
    ContextMenu,
    PageContent,
} from "components/shared";
import { svgs } from "utilities";
import { ExampleFlyoutContainer } from "./flyouts/exampleFlyout";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./pageWithFlyout.module.scss"));
const closedFlyoutState = { openFlyoutName: undefined };

export class PageWithFlyout extends Component {
    constructor(props) {
        super(props);
        this.state = closedFlyoutState;
    }

    closeFlyout = () => this.setState(closedFlyoutState);

    openFlyout = (name) => () => this.setState({ openFlyoutName: name });

    render() {
        const { t } = this.props,
            { openFlyoutName } = this.state,
            isExampleFlyoutOpen = openFlyoutName === "example";

        return (
            <ComponentArray>
                <ContextMenu>
                    <Btn
                        svg={svgs.reconfigure}
                        onClick={this.openFlyout("example")}
                    >
                        {t("walkthrough.pageWithFlyout.open")}
                    </Btn>
                </ContextMenu>
                <PageContent className={css("page-with-flyout-container")}>
                    {t("walkthrough.pageWithFlyout.pageBody")}
                    {isExampleFlyoutOpen && (
                        <ExampleFlyoutContainer onClose={this.closeFlyout} />
                    )}
                </PageContent>
            </ComponentArray>
        );
    }
}
