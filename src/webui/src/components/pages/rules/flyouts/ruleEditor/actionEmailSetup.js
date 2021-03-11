// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Trans } from "react-i18next";
import dot from "dot-object";

import { themedPaths } from "utilities";
import {
    ErrorMsg,
    Hyperlink,
    Indicator,
    ThemedSvgContainer,
} from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./actionEmailSetup.module.scss"));

export class ActionEmailSetup extends Component {
    constructor(props) {
        super(props);

        this.state = {
            clickedSetup: false,
        };

        if (!props.actionSettingsIsPending) {
            this.props.fetchActionSettings();
        }
    }

    clickedSetup = () => {
        this.props.pollActionSettings();
        this.setState({
            clickedSetup: true,
        });
    };

    render() {
        const {
                t,
                actionSettings = {},
                actionSettingsIsPending,
                actionSettingsError,
                actionIsPolling,
                actionPollingTimeout,
                applicationPermissionsAssigned,
            } = this.props,
            { clickedSetup } = this.state,
            actionsEmailIsEnabled = dot.pick("Email.isEnabled", actionSettings),
            actionsEmailSetupUrl = dot.pick(
                "Email.settings.office365ConnectorUrl",
                actionSettings
            ),
            showPending =
                clickedSetup && (actionSettingsIsPending || actionIsPolling),
            showError = !showPending && !!actionSettingsError,
            showSetupLink =
                !showPending &&
                !showError &&
                !actionsEmailIsEnabled &&
                !actionPollingTimeout,
            showSetupIncomplete =
                !showPending &&
                !showError &&
                !actionsEmailIsEnabled &&
                actionPollingTimeout,
            showSetupComplete =
                clickedSetup &&
                !showPending &&
                !showError &&
                actionsEmailIsEnabled;

        // Only show setup information if the application was deployed as owner,
        // as if it was not cannot verify whether or not setup is complete.
        // Also only show verification of setup if setup button was clicked.
        return applicationPermissionsAssigned ? (
            <div className={css("action-email-setup-container")}>
                {showPending && (
                    <div className={css("action-email-setup")}>
                        <Indicator
                            className={css("action-indicator")}
                            size="small"
                        />
                        <div className={css("info-message")}>
                            {t(
                                "rules.flyouts.ruleEditor.actions.checkingEmailSetup"
                            )}
                        </div>
                    </div>
                )}
                {showError && (
                    <div className={css("action-email-setup")}>
                        <ErrorMsg>
                            <Trans
                                i18nKey={
                                    "rules.flyouts.ruleEditor.actions.setupEmailError"
                                }
                            >
                                An error occurred.
                                <Hyperlink
                                    href={actionsEmailSetupUrl}
                                    onClick={this.clickedSetup}
                                    target="_blank"
                                >
                                    {t(
                                        "rules.flyouts.ruleEditor.actions.tryAgain"
                                    )}
                                </Hyperlink>
                            </Trans>
                        </ErrorMsg>
                    </div>
                )}
                {showSetupLink && (
                    <div className={css("action-email-setup")}>
                        <ThemedSvgContainer
                            className={css("icon")}
                            paths={themedPaths.infoBubble}
                        />
                        <div className={css("info-message")}>
                            <Trans
                                i18nKey={
                                    "rules.flyouts.ruleEditor.actions.setupEmail"
                                }
                            >
                                To send email alerts,
                                <Hyperlink
                                    href={actionsEmailSetupUrl}
                                    onClick={this.clickedSetup}
                                    target="_blank"
                                >
                                    {t(
                                        "rules.flyouts.ruleEditor.actions.outlookLogin"
                                    )}
                                </Hyperlink>
                                is required.
                            </Trans>
                        </div>
                    </div>
                )}
                {showSetupIncomplete && (
                    <div className={css("action-email-setup")}>
                        <ThemedSvgContainer
                            className={css("icon")}
                            paths={themedPaths.infoBubble}
                        />
                        <div className={css("info-message")}>
                            <Trans
                                i18nKey={
                                    "rules.flyouts.ruleEditor.actions.setupEmailTimeout"
                                }
                            >
                                Polling timed out.
                                <Hyperlink
                                    href={actionsEmailSetupUrl}
                                    onClick={this.clickedSetup}
                                    target="_blank"
                                >
                                    {t(
                                        "rules.flyouts.ruleEditor.actions.tryAgain"
                                    )}
                                </Hyperlink>
                            </Trans>
                        </div>
                    </div>
                )}
                {showSetupComplete && (
                    <div className={css("action-email-setup")}>
                        <ThemedSvgContainer
                            className={css("icon")}
                            paths={themedPaths.checkmarkBubble}
                        />
                        <div className={css("info-message")}>
                            {t(
                                "rules.flyouts.ruleEditor.actions.emailSetupConfirmed"
                            )}
                        </div>
                    </div>
                )}
            </div>
        ) : null;
    }
}
