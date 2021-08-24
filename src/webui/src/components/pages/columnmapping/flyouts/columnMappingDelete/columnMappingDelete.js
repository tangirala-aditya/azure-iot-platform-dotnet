// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";
import { ConfigService } from "services";
import { svgs } from "utilities";
import { permissions } from "services/models";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    Indicator,
    Protected,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummaryCount,
    SummarySection,
    Svg,
} from "components/shared";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./columnMappingDelete.module.scss"));

export class ColumnMappingDelete extends Component {
    constructor(props) {
        super(props);
        this.state = {
            impactedDeviceGroups: [],
            confirmStatus: false,
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            expandedValue: false,
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentDidMount() {
        if (this.props.deviceGroups) {
            this.populateDeviceGroupsState(this.props.deviceGroups);
        }
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
        if (
            nextProps.deviceGroups &&
            (this.props.deviceGroups || []).length !==
                nextProps.deviceGroups.length
        ) {
            this.populateDeviceGroupsState(nextProps.deviceGroups);
        }
    }

    componentWillUnmount() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    populateDeviceGroupsState = (deviceGroups = []) => {
        const impactedDeviceGroups = deviceGroups.filter(
            ({ mappingId }) => mappingId === this.props.columnMappingId
        );
        this.setState({
            impactedDeviceGroups: impactedDeviceGroups,
        });
    };

    toggleConfirm = (value) => {
        if (this.state.changesApplied) {
            this.setState({
                confirmStatus: value,
                changesApplied: false,
                successCount: 0,
            });
        } else {
            this.setState({ confirmStatus: value });
        }
    };

    deleteColumnMapping = (event) => {
        event.preventDefault();
        this.setState({ isPending: true, error: null });

        this.subscription = ConfigService.deleteColumnMapping(
            this.props.columnMappingId
        ).subscribe(
            (mappingId) => {
                this.props.deleteColumnMappings([mappingId]);
            },
            (error) =>
                this.setState({
                    error,
                    isPending: false,
                    changesApplied: true,
                }),
            () =>
                this.setState({
                    isPending: false,
                    changesApplied: true,
                    confirmStatus: false,
                })
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
        const { t, onClose } = this.props,
            {
                impactedDeviceGroups,
                confirmStatus,
                isPending,
                error,
                successCount,
                changesApplied,
            } = this.state,
            summaryCount = changesApplied
                ? successCount
                : impactedDeviceGroups.length,
            completedSuccessfully = changesApplied && !error;
        return (
            <Flyout
                header={t("columnMapping.delete.title")}
                t={t}
                onClose={onClose}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <Protected permission={permissions.updateDeviceGroups}>
                    <form
                        className={css("mapping-deletecontainer")}
                        onSubmit={this.deleteColumnMapping}
                    >
                        <div className={css("mapping-deleteheader")}>
                            {t("columnMapping.delete.header")}
                        </div>
                        <div className={css("mapping-deletedescr")}>
                            {t("columnMapping.delete.description")}
                        </div>
                        <Toggle
                            name="device-flyouts-delete"
                            attr={{
                                button: {
                                    "aria-label": t(
                                        "devices.flyouts.delete.header"
                                    ),
                                },
                            }}
                            on={confirmStatus}
                            onChange={this.toggleConfirm}
                            onLabel={t("columnMapping.delete.onLabel")}
                            offLabel={t("columnMapping.delete.offLabel")}
                        />
                        {this.state.impactedDeviceGroups.length > 0 && (
                            <SummarySection>
                                <SectionHeader>
                                    {t(
                                        "columnMapping.delete.affectedDeviceGroups"
                                    )}
                                </SectionHeader>
                                <SummaryBody>
                                    <div className={css("device-groups")}>
                                        <ul className={css("devicegroup-list")}>
                                            {this.state.impactedDeviceGroups.map(
                                                (dg, idx) => (
                                                    <li
                                                        className={css("item")}
                                                        key={dg.id}
                                                        data-index={dg.id}
                                                    >
                                                        <div
                                                            className={css(
                                                                "title"
                                                            )}
                                                            key={idx}
                                                            title={
                                                                dg.displayName
                                                            }
                                                        >
                                                            {dg.displayName}
                                                        </div>
                                                    </li>
                                                )
                                            )}
                                        </ul>
                                    </div>
                                </SummaryBody>
                            </SummarySection>
                        )}
                        <SummarySection>
                            <SectionHeader>
                                {t("columnMapping.delete.summaryHeader")}
                            </SectionHeader>
                            <SummaryBody>
                                <SummaryCount>{summaryCount}</SummaryCount>
                                <SectionDesc>
                                    {t(
                                        "columnMapping.delete.affectedDeviceGroups"
                                    )}
                                </SectionDesc>
                                {this.state.isPending && <Indicator />}
                                {completedSuccessfully && (
                                    <Svg
                                        className={css("summary-icon")}
                                        src={svgs.apply}
                                    />
                                )}
                            </SummaryBody>
                        </SummarySection>

                        {error && (
                            <AjaxError
                                className={css("mapping-deleteerror")}
                                t={t}
                                error={error}
                            />
                        )}
                        {!changesApplied && (
                            <BtnToolbar>
                                <Btn
                                    svg={svgs.trash}
                                    primary={true}
                                    disabled={isPending || !confirmStatus}
                                    type="submit"
                                >
                                    {t("columnMapping.delete.apply")}
                                </Btn>
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("columnMapping.delete.cancel")}
                                </Btn>
                            </BtnToolbar>
                        )}
                        {!!changesApplied && (
                            <BtnToolbar>
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("columnMapping.delete.close")}
                                </Btn>
                            </BtnToolbar>
                        )}
                    </form>
                </Protected>
            </Flyout>
        );
    }
}
