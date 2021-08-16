// Copyright (c) Microsoft. All rights reserved.

import React, { Fragment } from "react";

import { toDiagnosticsModel } from "services/models";
import { svgs, LinkedComponent, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    FormGroup,
    FormLabel,
    FormControl,
    PropertyRow as Row,
    PropertyCell as Cell,
    PropertyGrid as Grid,
    Indicator,
} from "components/shared";
import { ConfigService, IoTHubManagerService } from "services";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./columnMapping.module.scss"));

const toOption = (value, label) => ({
    label: label || value,
    value,
});

// A counter for creating unique keys per new condition
let conditionKey = 0;

const newColumnMapping = () => ({
    name: undefined,
    mapping: undefined,
    cellRenderer: undefined,
    description: "",
    key: conditionKey++, // Used by react to track the rendered elements
});

export class ColumnMapper extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            mappingOptions: [
                {
                    label: "lastActivity",
                    value: "lastActivity",
                },
                {
                    label: "isSimulated",
                    value: "isSimulated",
                },
                {
                    label: "c2DMessageCount",
                    value: "c2DMessageCount",
                },
                {
                    label: "enabled",
                    value: "enabled",
                },
                {
                    label: "lastStatusUpdated",
                    value: "lastStatusUpdated",
                },
                {
                    label: "ioTHubHostName",
                    value: "ioTHubHostName",
                },
                {
                    label: "eTag",
                    value: "eTag",
                },
                {
                    label: "authentication",
                    value: "authentication",
                },
            ],
            rendererOptions: [
                { label: "SimulatedRenderer", value: "IsSimulatedRenderer" },
                {
                    label: "ConnectionStatusRenderer",
                    value: "ConnectionStatusRenderer",
                },
                { label: "TimeRenderer", value: "TimeRenderer" },
                { label: "DefaultRenderer", value: "DefaultRenderer" },
            ],
            mappingId: this.props.mappingId || "",
            isEdit: this.props.isEdit || false,
            isDefault: this.props.isDefault || false,
            isAdd: this.props.isAdd || false,
            mappingName: (this.props.columnMapping || {}).name,
            eTag: (this.props.columnMapping || {}).eTag,
            columnMappings: (this.props.columnMapping || {}).mapping || [],
            defaultColumnMappings:
                (this.props.defaultColumnMapping || {}).mapping || [],
            isPending: true,
            error: undefined,
        };

        // State to input links
        this.mappingsLink = this.linkTo("columnMappings");
        this.defaultMappingsLink = this.linkTo("defaultColumnMappings");
        this.mappingNameLink = this.linkTo("mappingName").check(
            Validator.notEmpty,
            this.props.t("deviceQueryConditions.errorMsg.fieldCantBeEmpty")
        );
    }

    conditionIsNew(condition) {
        return !condition.field && !condition.operator && !condition.value;
    }

    formIsValid() {
        return [this.mappingsLink].every((link) => !link.error);
    }

    componentDidMount() {
        this.subscription =
            IoTHubManagerService.getDeviceProperties().subscribe(
                (items) => {
                    const filterOptions = items.map((item) => toOption(item));
                    this.setState({
                        mappingOptions: [
                            ...this.state.mappingOptions,
                            ...filterOptions,
                        ],
                    });
                },
                (filtersError) => this.setState({ filtersError })
            );

        if (!this.props.isPending) {
            this.setState({
                mappingName: (this.props.columnMapping || {}).name,
                eTag: (this.props.columnMapping || {}).eTag,
                columnMappings: (this.props.columnMapping || {}).mapping || [],
                defaultColumnMappings:
                    (this.props.defaultColumnMapping || {}).mapping || [],
                isPending: this.props.isPending,
            });
        }
    }

    static getDerivedStateFromProps(props, state) {
        if (props.isPending !== state.isPending) {
            if (!props.isPending) {
                return {
                    mappingName: (props.columnMapping || {}).name,
                    eTag: (props.columnMapping || {}).eTag,
                    columnMappings: (props.columnMapping || {}).mapping || [],
                    defaultColumnMappings:
                        (props.defaultColumnMapping || {}).mapping || [],
                    isPending: props.isPending,
                };
            } else {
                return {
                    isPending: props.isPending,
                };
            }
        }

        return null;
    }

    componentWillUnmount() {
        if (this.subscription) this.subscription.unsubscribe();
    }

    apply = (event) => {
        event.preventDefault();

        var requestData = {
            Id: this.state.mappingId,
            ETag: this.state.eTag,
            ColumnMappingDefinitions: this.state.columnMappings,
            Name: this.state.mappingName,
            IsDefault: this.state.isDefault,
        };
        if (this.state.mappingId && this.state.eTag) {
            ConfigService.updateColumnMappings(
                this.state.mappingId,
                requestData
            ).subscribe(
                (columnMapping) => {
                    this.props.fetchColumnMappings();
                    if (this.state.isDefault) {
                        this.props.history.push(`/columnMapping/default`);
                    } else {
                        this.props.history.push(`/columnMapping/custom`);
                    }
                },
                (error) => {}
            );
        } else {
            ConfigService.createColumnMappings(requestData).subscribe(
                (columnMapping) => {
                    this.props.fetchColumnMappings();
                    if (this.state.isDefault) {
                        this.props.history.push(`/columnMapping/default`);
                    } else {
                        this.props.history.push(`/columnMapping/custom`);
                    }
                },
                (error) => {}
            );
        }
    };

    addCondition = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateColumnMapping_AddColumnMapping", {})
        );
        return this.mappingsLink.set([
            ...this.mappingsLink.value,
            newColumnMapping(),
        ]);
    };

    deleteCondition = (index) => () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateColumnMapping_DeleteColumnMapping", {})
        );
        return this.mappingsLink.set(
            this.mappingsLink.value.filter((_, idx) => index !== idx)
        );
    };

    resetColumnMappings = () => {
        this.setState(
            {
                columnMappings: (this.props.columnMapping || {}).mapping || [],
            },
            () => {
                this.render();
            }
        );
    };

    onReset = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateColumnMappings_Reset", {})
        );
        this.resetColumnMappings();
    };

    onCancel = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateColumnMappings_Cancel", {})
        );
        if (this.state.isDefault) {
            this.props.history.push(`/columnMapping/default`);
        } else {
            this.props.history.push(`/columnMapping/custom`);
        }
    };

    operatorOptionArr = (options, key) => {
        var optionArr = options;
        if (
            this.state.deviceQueryConditions.length > 0 &&
            this.state.deviceQueryConditions[key].field !== "deviceId"
        ) {
            optionArr = optionArr.filter((option) => option.value !== "LK");
            if (this.state.deviceQueryConditions[key].operator === "LK") {
                this.state.deviceQueryConditions[key].operator = undefined;
            }
        }

        return optionArr;
    };

    onFieldChange = (key) => {
        // if (
        //     this.state.deviceQueryConditions[key].field !== "connectionState" &&
        //     (this.state.deviceQueryConditions[key].value === "Connected" ||
        //         this.state.deviceQueryConditions[key].value === "Disconnected")
        // ) {
        //     this.state.deviceQueryConditions[key].value = "";
        // }
    };

    render() {
        const { t } = this.props,
            // Create the state link for the dynamic form elements
            mappingsLink = this.mappingsLink.getLinkedChildren(
                (conditionLink) => {
                    let name = conditionLink
                            .forkTo("name")
                            .check(
                                Validator.notEmpty,
                                t("columnMapping.errorMsg.headerCannotBeEmpty")
                            )
                            .check(
                                Validator.notDuplicated,
                                t("columnMapping.errorMsg.nameIsDuplicated"),
                                !this.state.isDefault
                                    ? [
                                          ...this.state.defaultColumnMappings,
                                          ...this.state.columnMappings,
                                      ].map((m) => m.name)
                                    : this.state.columnMappings.map(
                                          (m) => m.name
                                      )
                            ),
                        mapping = conditionLink
                            .forkTo("mapping")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t("columnMapping.errorMsg.mappingCannotBeEmpty")
                            )
                            .check(
                                Validator.notDuplicated,
                                t("columnMapping.errorMsg.mappingIsDuplicated"),
                                !this.state.isDefault
                                    ? [
                                          ...this.state.defaultColumnMappings,
                                          ...this.state.columnMappings,
                                      ].map((m) => m.mapping)
                                    : this.state.columnMappings.map(
                                          (m) => m.mapping
                                      )
                            ),
                        renderer = conditionLink
                            .forkTo("cellRenderer")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "columnMapping.errorMsg.rendererCannotBeEmpty"
                                )
                            ),
                        description = conditionLink.forkTo("description"),
                        edited = !(
                            !name.value &&
                            !mapping.value &&
                            !renderer.value &&
                            !description.value
                        );
                    let error =
                        name.error ||
                        mapping.error ||
                        renderer.error ||
                        description.error ||
                        "";
                    return {
                        name,
                        mapping,
                        renderer,
                        description,
                        edited,
                        error,
                    };
                }
            ),
            defaultMappingsLink = this.defaultMappingsLink.getLinkedChildren(
                (conditionLink) => {
                    let name = conditionLink
                            .forkTo("name")
                            .check(
                                Validator.notEmpty,
                                t("columnMapping.errorMsg.headerCannotBeEmpty")
                            )
                            .check(
                                Validator.notDuplicated,
                                t("columnMapping.errorMsg.nameIsDuplicated"),
                                this.state.columnMappings.map((m) => m.name)
                            ),
                        mapping = conditionLink
                            .forkTo("mapping")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t("columnMapping.errorMsg.mappingCannotBeEmpty")
                            )
                            .check(
                                Validator.notDuplicated,
                                t("columnMapping.errorMsg.mappingIsDuplicated"),
                                this.state.columnMappings.map((m) => m.mapping)
                            ),
                        renderer = conditionLink
                            .forkTo("cellRenderer")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "columnMapping.errorMsg.rendererCannotBeEmpty"
                                )
                            ),
                        description = conditionLink.forkTo("description"),
                        edited = !(
                            !name.value &&
                            !mapping.value &&
                            !renderer.value &&
                            !description.value
                        );
                    let error =
                        name.error ||
                        mapping.error ||
                        renderer.error ||
                        description.error ||
                        "";
                    return {
                        name,
                        mapping,
                        renderer,
                        description,
                        edited,
                        error,
                    };
                }
            ),
            conditionHasErrors =
                defaultMappingsLink.some(({ error }) => !!error) ||
                mappingsLink.some(({ error }) => !!error);
        return (
            <Fragment>
                {!(this.state.isDefault || this.state.isEdit) && (
                    <FormGroup>
                        <FormLabel>
                            {t("columnMapping.labels.mappingName")}
                        </FormLabel>
                        <FormControl
                            type="text"
                            aria-label={t("columnMapping.field")}
                            className="long"
                            searchable="false"
                            clearable="false"
                            placeholder={t("columnMapping.namePlaceHolder")}
                            link={this.mappingNameLink}
                        />
                    </FormGroup>
                )}
                {!this.state.isDefault && this.state.isEdit && (
                    <p>{this.state.mappingName}</p>
                )}
                {this.state.isPending && (
                    <Indicator size="large" pattern="bar" />
                )}
                <form onSubmit={this.apply}>
                    <div className={css("manage-filters-container")}>
                        <Grid>
                            {(defaultMappingsLink.length > 0 ||
                                mappingsLink.length > 0) && (
                                <Row>
                                    <Cell className="col-1 button"></Cell>
                                    <Cell className="col-3">Name</Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-2">Mapping</Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-2">Renderer</Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-3">Description</Cell>
                                </Row>
                            )}
                            {!this.state.isDefault &&
                                defaultMappingsLink.map((condition, idx) => (
                                    <Row
                                        key={idx}
                                        // className="deviceExplorer-conditions"
                                    >
                                        <Cell className="col-1 button"></Cell>
                                        <Cell className="col-3">
                                            <FormControl
                                                type="text"
                                                aria-label={t(
                                                    "columnMapping.field"
                                                )}
                                                className="long"
                                                disabled={true}
                                                searchable="false"
                                                clearable="false"
                                                placeholder={t(
                                                    "columnMapping.headerPlaceholder"
                                                )}
                                                link={condition.name}
                                                onChange={this.onFieldChange(
                                                    idx
                                                )}
                                            />
                                        </Cell>
                                        <Cell className="col-1"></Cell>
                                        <Cell className="col-2">
                                            <FormControl
                                                type="select"
                                                aria-label={t(
                                                    "columnMapping.mapping"
                                                )}
                                                className="long"
                                                disabled={true}
                                                searchable="false"
                                                clearable="false"
                                                options={
                                                    this.state.mappingOptions
                                                }
                                                placeholder={t(
                                                    "columnMapping.mappingPlaceholder"
                                                )}
                                                link={condition.mapping}
                                            />
                                        </Cell>
                                        <Cell className="col-1"></Cell>
                                        <Cell className="col-2">
                                            <FormControl
                                                type="select"
                                                aria-label={t(
                                                    "columnMapping.renderer"
                                                )}
                                                className="long"
                                                disabled={true}
                                                searchable="false"
                                                clearable="false"
                                                options={
                                                    this.state.rendererOptions
                                                }
                                                placeholder={t(
                                                    "columnMapping.renderPlaceholder"
                                                )}
                                                link={condition.renderer}
                                            />
                                        </Cell>
                                        <Cell className="col-1"></Cell>
                                        <Cell className="col-3">
                                            <FormControl
                                                type="text"
                                                placeholder={t(
                                                    "columnMapping.descriptionPlaceholder"
                                                )}
                                                link={condition.description}
                                                className={css("width-70")}
                                            />
                                        </Cell>
                                    </Row>
                                ))}
                            {mappingsLink.map((condition, idx) => (
                                <Row
                                    key={idx}
                                    // className="deviceExplorer-conditions"
                                >
                                    <Cell className="col-1 button">
                                        <Btn
                                            className="btn-icon"
                                            icon="cancel"
                                            onClick={this.deleteCondition(idx)}
                                        />
                                    </Cell>
                                    <Cell className="col-3">
                                        <FormControl
                                            type="text"
                                            aria-label={t(
                                                "columnMapping.field"
                                            )}
                                            className="long"
                                            searchable="false"
                                            clearable="false"
                                            placeholder={t(
                                                "columnMapping.headerPlaceholder"
                                            )}
                                            link={condition.name}
                                            onChange={this.onFieldChange(idx)}
                                        />
                                    </Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-2">
                                        <FormControl
                                            type="select"
                                            aria-label={t(
                                                "columnMapping.operator"
                                            )}
                                            className="long"
                                            searchable="false"
                                            clearable="false"
                                            options={this.state.mappingOptions}
                                            placeholder={t(
                                                "columnMapping.mappingPlaceholder"
                                            )}
                                            link={condition.mapping}
                                        />
                                    </Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-2">
                                        <FormControl
                                            type="select"
                                            aria-label={t(
                                                "columnMapping.operator"
                                            )}
                                            className="long"
                                            searchable="false"
                                            clearable="false"
                                            options={this.state.rendererOptions}
                                            placeholder={t(
                                                "columnMapping.renderPlaceholder"
                                            )}
                                            link={condition.renderer}
                                        />
                                    </Cell>
                                    <Cell className="col-1"></Cell>
                                    <Cell className="col-3">
                                        <FormControl
                                            type="text"
                                            placeholder={t(
                                                "columnMapping.descriptionPlaceholder"
                                            )}
                                            link={condition.description}
                                            className={css("width-70")}
                                        />
                                    </Cell>
                                </Row>
                            ))}
                        </Grid>
                        <Btn
                            className={css("add-btn")}
                            svg={svgs.plus}
                            onClick={this.addCondition}
                        >
                            Add a Mapping
                        </Btn>
                        <div className={css("cancel-right-div")}>
                            <BtnToolbar>
                                <Btn
                                    primary
                                    disabled={
                                        !this.formIsValid() ||
                                        conditionHasErrors ||
                                        (!this.state.isDefault &&
                                            this.mappingNameLink.error) ||
                                        this.state.isPending ||
                                        this.state.columnMappings.length === 0
                                    }
                                    type="submit"
                                >
                                    Save
                                </Btn>
                                <Btn
                                    disabled={this.state.isPending}
                                    svg={svgs.cancelX}
                                    onClick={this.onReset}
                                >
                                    Reset
                                </Btn>
                                {!this.state.isDefault && (
                                    <Btn
                                        svg={svgs.cancelX}
                                        onClick={this.onCancel}
                                    >
                                        Cancel
                                    </Btn>
                                )}
                            </BtnToolbar>
                        </div>
                        {this.state.error && (
                            <AjaxError t={t} error={this.state.error} />
                        )}
                    </div>
                </form>
            </Fragment>
        );
    }
}
