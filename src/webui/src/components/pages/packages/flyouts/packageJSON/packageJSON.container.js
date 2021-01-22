// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { PackageJSON } from "./packageJSON";
import { getTheme } from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
    theme: getTheme(state),
});

export const PackageJSONContainer = withTranslation()(
    connect(mapStateToProps, null)(PackageJSON)
);
